using System;

namespace BarodaPOS.Models
{
    public enum FastLoginMode
    {
        None = 0,
        Forever = 1,
        FastPIN = 2
    }

    public class LoginSettingsModel
    {
        public FastLoginMode Mode { get; set; } = FastLoginMode.None;
        public string SavedUsername { get; set; } = string.Empty;
        public string FastPIN { get; set; } = string.Empty; // In a real world scenario this would be hashed
    }
}
