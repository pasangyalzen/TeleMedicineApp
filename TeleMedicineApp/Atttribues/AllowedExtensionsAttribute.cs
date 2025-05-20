using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace TeleMedicineApp.Attributes // Change this to match your project namespace
{
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var file = value as IFormFile;
            if (file != null)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult($"Only the following file types are allowed: {string.Join(", ", _extensions)}");
                }
            }

            return ValidationResult.Success;
        }
    }
}