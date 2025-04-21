using Supabase;
using BlazorBankingApp.Services;
using System;
using System.Threading.Tasks;
using Supabase.Gotrue;

namespace BlazorBankingApp.Service
{
    public class TransactionService
    {
        private readonly SupabaseService _supabaseService;

        public TransactionService(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
        }

        // Method to handle sending money
        public async Task<bool> SendMoney(string senderId, string recipientId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero.");
            }

            try
            {
                // Fetch sender and recipient balances
                var sender = await GetUserById(senderId);
                var recipient = await GetUserById(recipientId);

                if (sender == null || recipient == null)
                {
                    throw new InvalidOperationException("Sender or recipient not found.");
                }

                if (sender.Balance < amount)
                {
                    throw new InvalidOperationException("Insufficient balance.");
                }

                // Update balances
                sender.Balance -= amount;
                recipient.Balance += amount;

                // Save updated balances to the database
                await UpdateUserBalance(senderId, sender.Balance);
                await UpdateUserBalance(recipientId, recipient.Balance);

                // Log the transaction
                await LogTransaction(senderId, recipientId, amount);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending money: {ex.Message}");
                return false;
            }
        }

        // Method to log a transaction
        private async Task LogTransaction(string senderId, string recipientId, decimal amount)
        {
            var transaction = new
            {
                SenderId = senderId,
                RecipientId = recipientId,
                Amount = amount,
                Timestamp = DateTime.UtcNow
            };

            /*await _supabaseService.GetClient()
                .From("transactions")
                .Insert(transaction);*/
        }

        // Method to fetch a user by ID
        private async Task<BankUser> GetUserById(string userId)
        {
            return await _supabaseService.GetClient()
                .From<BankUser>()
                .Select("*")
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                .Single();
        }

        // Method to update a user's balance
        private async Task UpdateUserBalance(string userId, decimal newBalance)
        {
            var updateData = new { Balance = newBalance };

            /*await _supabaseService.GetClient()
                .From("users")
                .Update(updateData)
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId);*/
        }
    }
}