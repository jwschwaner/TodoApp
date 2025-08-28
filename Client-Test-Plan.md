# TodoApp Client Test Plan

Purpose
- Provide browser-only, manual test cases you can run against the Blazor Server client to verify the assignment requirements before your presentation.
- Mapped to: Initial app, HTTPS, Hashing, and Symmetric/Asymmetric encryption requirement PDFs.

Assumptions and setup
- You can run the app locally over HTTPS (Kestrel) at https://localhost:7181 and HTTP at http://localhost:5204.
- You have at least one confirmed test account to sign in with. If “Require confirmed account” blocks login after registration, use an already confirmed account you created earlier.
- Optional: An Admin user exists for admin-only tests. If not, skip the Admin section or promote a user beforehand.

How to start the app (for these client tests)
- Start Kestrel build:
  1) Ensure Postgres is running (Docker compose brings it up automatically when you run the HTTPS script). Or start just Postgres:
     - Open a terminal in repo root and run: `cd Docker && docker compose up -d postgres`
  2) Start the Blazor app:
     - Open another terminal in repo root and run: `cd TodoApp && dotnet run`
  3) Browse to https://localhost:7181

Note on HTTPS: Your browser should show a secure padlock when the dev cert is trusted.

Legend
- Step: precise action to perform in the browser.
- Expected: what you should see if the requirement is met.

---

1) HTTPS requirements (client-observable)
- Step: In the browser, navigate to http://localhost:5204.
  - Expected: You receive an HTTP 3xx redirect to https://localhost:7181 and end up on HTTPS.
- Step: Verify the URL bar shows https://localhost:7181 and the page loads.
  - Expected: A secure padlock is shown (trusted certificate) and the app renders the Home/landing content.
- Step: Open a private/incognito window and repeat the two previous steps.
  - Expected: Same redirect and secure padlock are observed (no cached state required).

Coverage mapping: HTTPS-Requirements.pdf (HTTP→HTTPS redirect; app serves over HTTPS; browser trust/padlock visible).

---

2) Initial app and authentication
2.1 Anonymous landing and protected pages
- Step: With no login session, browse to https://localhost:7181/home.
  - Expected: You see the NotAuthorized landing content with prominent Register and Sign In buttons.
- Step: In the address bar, go directly to https://localhost:7181/cpr.
  - Expected: Because CPR page is [Authorize], you’re redirected to the login experience.
- Step: In the address bar, go directly to https://localhost:7181/todo.
  - Expected: You’re redirected to the login experience.

2.2 Register (if you don’t already have a confirmed account)
- Step: Click “Get Started” or go to /Account/Register and register a test user.
  - Expected: Registration completes and you are informed to confirm your email before signing in (if confirmation is required). If you already have a confirmed user, skip this.

2.3 Sign in
- Step: Go to /Account/Login and sign in with your confirmed test user.
  - Expected: You’re authenticated and taken to the app. Home shows “Welcome, <email>!” and user-specific tiles (Profile, Security, Email Settings). You see a link to Admin only if you’re in the Admin role.

Coverage mapping: Initial-app-requirements.pdf (landing page, auth endpoints, protected routes require login).

---

3) CPR registration and validation
3.1 Redirect behavior when CPR is missing
- Step: After login, visit https://localhost:7181/todo directly.
  - Expected: You’re redirected to /cpr because you have no CPR yet.

3.2 Client-side validation
- Step: On /cpr, leave the input empty and click “Save CPR Number”.
  - Expected: Validation message: “CPR Number is required.” No navigation occurs.
- Step: Enter fewer than 10 characters (e.g., 12345) and click Save.
  - Expected: “CPR Number must be exactly 10 digits.”
- Step: Enter 10 chars with non-digits (e.g., 12345abcde) and click Save.
  - Expected: “CPR Number must contain only numbers.”

3.3 Happy path
- Step: Enter a valid 10-digit number (e.g., 1234567890) and click Save.
  - Expected: You see a brief saving state, then you are navigated to /todo automatically.

3.4 Duplicate CPR across users (requires two users)
- Step: Log out. Log in as another user (User B) without CPR yet.
- Step: Go to /cpr and enter the exact same 10-digit number used by User A.
  - Expected: An error alert appears: “Failed to save CPR number. Please try again.” You remain on /cpr.
- Step: Enter a different 10-digit number and save.
  - Expected: Success and navigation to /todo for User B.

Coverage mapping: Hashing-requirements.pdf (CPR is handled without exposing plaintext to users; duplicate CPR prevented across users); Initial-app-requirements.pdf (navigation gating by CPR presence).

---

4) Todo list behaviors (per CPR)
4.1 First load and state
- Step: After successful CPR save, the /todo page loads.
  - Expected: You see an empty list message if no todos exist. A small text shows “Your hashed CPR number: <value>”. This value must not equal your 10-digit input.

4.2 Adding todos
- Step: Type a short todo like “Write tests” and click Add (or press Enter).
  - Expected: The item appears in the list. The input clears. No error is shown.
- Step: Try to add an empty or whitespace-only todo.
  - Expected: Inline error: “Please enter a todo.”
- Step: Try a very long todo (>200 characters).
  - Expected: Inline error: “Todo is too long (max 200 characters).”

4.3 Toggle done and delete
- Step: Click the checkbox next to a todo.
  - Expected: The label toggles strike-through; reloading the page keeps the updated state.
- Step: Click the trash icon for a todo.
  - Expected: The item disappears; reloading the page keeps it gone.

4.4 Isolation by hashed CPR
- Step: Log out from User A; log in as User B (who has a distinct CPR).
  - Expected: User B’s /todo list does not show User A’s items; their own list is independent.

Coverage mapping: Symetrisk_asymetrisk_kryptering-requirements.pdf (client can consistently read todos across reloads, indicating server decrypts encrypted-at-rest data successfully); Hashing-requirements.pdf (displayed CPR value is hashed; isolation is by hashed key, not plaintext).

---

5) Authorization and Admin-only features (optional if you have an Admin)
5.1 Non-admin access denied
- Step: As a normal user, go to https://localhost:7181/admin/users.
  - Expected: You’re denied access or redirected to login (no Admin role).

5.2 Admin user listing and actions
- Step: Sign in as an Admin and go to /admin/users.
  - Expected: You see a table of users (excluding yourself), with buttons to View todos and Delete user.
- Step: Click the eye icon for a user who has a CPR and some todos.
  - Expected: Their todo list renders. You can toggle items done and delete them; changes persist on reload.
- Step: Click Delete user and confirm.
  - Expected: The user is removed from the table. If they were selected, their todo list clears. Their account should no longer be able to sign in.

Coverage mapping: Initial-app-requirements.pdf (RBAC); Symetrisk_asymetrisk_kryptering-requirements.pdf (admin can view decrypted items—server decrypts).

---

6) Security-visible checks from the client (sanity verifications)
- CPR secrecy in UI: Nowhere in the UI is the raw 10-digit CPR shown after saving. Only a non-plaintext, hashed-looking value is displayed on /todo. If you see the exact 10 digits anywhere after creation, that’s a failure.
- Cross-user isolation: Logging in as a different user never shows another user’s todos, even if the same raw CPR was tried (duplicate CPR is rejected).
- Consistent decryption: Refreshing /todo shows the same readable text you added earlier; this implies server-side decrypt is working reliably.

Coverage mapping: Hashing-requirements.pdf; Symetrisk_asymetrisk_kryptering-requirements.pdf.

---

7) Docker (client checks, optional)
- Step: Bring up Docker app: `cd Docker && docker compose up --build -d`
  - Expected: After a short wait, https://localhost:5003 is reachable; http://localhost:5002 redirects to HTTPS.
- Step: Repeat key client flows on https://localhost:5003: login, CPR, todos.
  - Expected: Identical behavior to Kestrel instance, including secure padlock.

Coverage mapping: HTTPS-Requirements.pdf (Docker HTTPS + redirect mirrors Kestrel).

---

8) Negative and edge cases (quick hits)
- Session loss during action: On /todo, sign out in another tab and then try to add a todo in the first tab.
  - Expected: You’re redirected to login (token/session enforced).
- CPR page reload while saving: On /cpr, click Save and immediately refresh the page.
  - Expected: No crash; either you’re on /todo (if saved) or back on /cpr (if not), with normal behavior.
- Rapid toggling of done: Click a todo’s checkbox on/off a few times quickly.
  - Expected: UI stays responsive; final state persists after a reload.

---

Requirements coverage summary
- Initial-app-requirements.pdf
  - Anonymous landing with Register/Login: Covered (2.1)
  - Protected routes require auth: Covered (2.1)
  - Basic navigation and pages (Home/CPR/Todo): Covered (2–4)
  - RBAC (Admin area visibility and restriction): Covered (5)
- HTTPS-Requirements.pdf
  - HTTP→HTTPS redirect: Covered (1, 7)
  - Browser shows HTTPS with a trusted certificate: Covered (1, 7)
- Hashing-requirements.pdf
  - CPR not stored/shown as plaintext in client views: Covered (4.1, 6)
  - Duplicate CPR across users rejected: Covered (3.4)
  - Isolation by hashed key (different users see only their todos): Covered (4.4)
- Symetrisk_asymetrisk_kryptering-requirements.pdf
  - Todos readable across reloads (implies decrypt works): Covered (4.1–4.3, 6)
  - Admin can view and manage user todos (server decrypt): Covered (5.2)

Notes
- Some server internals (actual cryptographic algorithms, key management, IIS lockout) can’t be directly proven via client steps; they’re validated by the provided automation and integration tests. Use this client plan to demonstrate observable outcomes that correspond to those requirements.

