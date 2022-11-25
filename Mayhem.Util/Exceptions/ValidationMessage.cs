namespace Mayhem.Util.Exceptions
{
    public class ValidationMessage
    {
        public ValidationMessage(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; set; }

        public string Message { get; set; }
    }
}