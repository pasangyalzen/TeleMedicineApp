namespace TeleMedicineApp
{
    public class OperationResponse<T>
    {
        public List<string> ErrorMessage { get; } = new List<string>();

        // Add an error message
        public void AddError(string Message)
        {
            ErrorMessage.Add(Message);
        }

        // Add multiple error messages
        public void AddError(List<string> Messages)
        {
            ErrorMessage.AddRange(Messages);
        }

        // Corrected IsSuccess property
        public bool IsSuccess { get { return ErrorMessage.Count() == 0; } }

        // The result of the operation
        public T Result { get; set; }

        // Message associated with the result
        public string ResultMessage { get; set; }
    }
}