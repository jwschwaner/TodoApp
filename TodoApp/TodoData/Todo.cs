namespace TodoApp.TodoData
{
    public class Todo
    {
        public Guid Id { get; set; }
        public string CprNr { get; set; }
        public byte[] EncryptedItem { get; set; }
        public string Item { get; set; } // decrypted at runtime
        public bool IsDone { get; set; }
        
        // Navigation property
        public Cpr Cpr { get; set; }
    }
}
