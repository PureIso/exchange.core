namespace exchange.core.interfaces.models
{
    public interface IDatabase
    {
        string FileName { get; set; }

        void SaveChanges();
        void WriteFile(string filename, string data);
        void Load();
    }
}
