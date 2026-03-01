namespace Sabro.Exeptions
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message) : base(message)
        {
        }

        public ConcurrencyException()
            : base("The record has been modified by another user. Please refresh and try again.")
        {
        }
    }
}
