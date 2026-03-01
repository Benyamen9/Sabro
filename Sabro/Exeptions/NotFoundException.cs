namespace Sabro.Exeptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException(string resourceType, int id)
            : base($"{resourceType} with ID {id} not found")
        {
        }
    }
}
