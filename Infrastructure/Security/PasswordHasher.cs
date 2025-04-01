using System;
using System.Security.Cryptography;
using System.Text;

namespace WPFGrowerApp.Infrastructure.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int HashSize = 32; // 256 bit
        private const int Iterations = 10000; // Number of iterations for PBKDF2

        /// <summary>
        /// Creates a hash from a password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <returns>A tuple containing the hash (Base64) and salt (Base64).</returns>
        public static (string Hash, string Salt) HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            // Create salt
            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string saltBase64 = Convert.ToBase64String(saltBytes);

            // Create hash
            byte[] hashBytes = ComputePbkdf2Hash(password, saltBytes);
            string hashBase64 = Convert.ToBase64String(hashBytes);

            return (hashBase64, saltBase64);
        }

        /// <summary>
        /// Verifies a password against a stored hash and salt.
        /// </summary>
        /// <param name="password">The password to check.</param>
        /// <param name="storedHashBase64">The stored hash (Base64).</param>
        /// <param name="storedSaltBase64">The stored salt (Base64).</param>
        /// <returns>True if the password is correct, otherwise false.</returns>
        public static bool VerifyPassword(string password, string storedHashBase64, string storedSaltBase64)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHashBase64) || string.IsNullOrEmpty(storedSaltBase64))
            {
                return false; // Or throw ArgumentNullException depending on desired behavior
            }

            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSaltBase64);
                byte[] storedHashBytes = Convert.FromBase64String(storedHashBase64);

                // Compute hash of the provided password using the stored salt
                byte[] computedHashBytes = ComputePbkdf2Hash(password, saltBytes);

                // Compare the computed hash with the stored hash
                // Use constant-time comparison to prevent timing attacks
                return SlowEquals(storedHashBytes, computedHashBytes);
            }
            catch (FormatException)
            {
                // Handle invalid Base64 strings if necessary
                Infrastructure.Logging.Logger.Warn("Invalid Base64 format encountered during password verification.");
                return false;
            }
            catch (Exception ex)
            {
                 Infrastructure.Logging.Logger.Error("Error during password verification.", ex);
                 return false;
            }
        }

        private static byte[] ComputePbkdf2Hash(string password, byte[] salt)
        {
            // Note: In .NET 6+, Rfc2898DeriveBytes defaults to SHA256. For older frameworks, specify HashAlgorithmName.SHA256.
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        /// <summary>
        /// Constant-time comparison of two byte arrays.
        /// </summary>
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}
