using Supabase;
using BlazorBankingApp.Services;
using System;
using System.Threading.Tasks;

namespace BlazorBankingApp.Service
{
    public class DataService {
        private readonly SupabaseService _supabaseService;
        private readonly UserService _userService;

        public DataService(SupabaseService supabaseService, UserService userService) {
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

                await _userService.SetBalance();
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

                return _userService.name ?? "Unknown";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching name: {ex.Message}");
                throw;
            }
        }
    }
}