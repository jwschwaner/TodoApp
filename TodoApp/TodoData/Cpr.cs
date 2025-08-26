namespace TodoApp.TodoData
{
    public class Cpr
    {
        public string UserId { get; set; }
        public string CprNr { get; set; }
        
        // Navigation property
        public ICollection<Todo> Todos { get; set; }
    }
}
