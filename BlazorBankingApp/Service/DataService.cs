using Supabase;
using BlazorBankingApp.Services;
using System;
using System.Threading.Tasks;

namespace BlazorBankingApp.Service
{
    public class DataService
    {
        private readonly SupabaseService _supabaseService;
        private readonly UserService _userService;

        public DataService(SupabaseService supabaseService, UserService userService)
        {
            _supabaseService = supabaseService;
            _userService = userService;
        }

        public async Task<float> FetchBalance()
        {
            try
            {
                var session = _supabaseService.GetClient().Auth.CurrentSession;
                if (session == null)
                {
                    throw new InvalidOperationException("User session is not available. Please log in again.");
                }

                using var supabaseService = new SupabaseService();
                await supabaseService.RestoreSession();
                BankUser bankUser = await supabaseService.GetClient()
                        .From<BankUser>()
                        .Select("balance")
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, supabaseService.GetClient().Auth.CurrentSession?.User?.Id ?? throw new InvalidOperationException("User is not authenticated."))
                        .Single();

                if (bankUser != null)
                {
                    Console.WriteLine($"Loaded user: {supabaseService.GetClient().Auth.CurrentSession?.User?.Email} Balance: {bankUser.Balance}");
                    return (float)bankUser.Balance;
                }
                else
                {
                    Console.WriteLine("Failed to load user balance.");
                }
            return _userService.balance;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching balance: {ex.Message}");
                throw;
            }
        }

        public async Task<string> FetchName()
        {
            try
            {
                var session = _supabaseService.GetClient().Auth.CurrentSession;
                if (session == null)
                {
                    throw new InvalidOperationException("User session is not available. Please log in again.");
                }

                return _userService.name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching name: {ex.Message}");
                throw;
            }
        }
    }
}