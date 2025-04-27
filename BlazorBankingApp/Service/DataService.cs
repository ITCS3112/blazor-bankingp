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
                if (session == null || session.User == null)
                {
                    throw new InvalidOperationException("User session is not available. Please log in again.");
                }
                Console.WriteLine($"Session restored. User ID: {session.User.Id}");

                using var supabaseService = _supabaseService;
                await supabaseService.RestoreSession();

                Console.WriteLine($"Fetching balance for user ID: {session.User.Id}");

                BankUser bankUser = await supabaseService.GetClient()
                    .From<BankUser>()
                    .Select("balance")
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, session.User.Id)
                    .Single();


                //If the count == 0 then the user is not in the BankUser table
                var debugResponse = await supabaseService.GetClient()
                    .From<BankUser>()
                    .Select("*")
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, session.User.Id)
                    .Get();

                Console.WriteLine($"Debug query result count: {debugResponse.Models.Count}");

                if (bankUser != null)
                {
                    _userService.balance = (float)bankUser.Balance;
                    Console.WriteLine($"Loaded user: {supabaseService.GetClient().Auth.CurrentSession?.User?.Email} Balance: {_userService.balance}");
                    return _userService.balance;
                }
                else
                {
                    Console.WriteLine("Failed to load user balance.");
                }
                return 0f; //means users balance was not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching balance: {ex.Message}");
                throw;
            }
        }
        public async Task<string> FetchAuthority()
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
                        .Select("authoritylevel")
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, supabaseService.GetClient().Auth.CurrentSession?.User?.Id ?? throw new InvalidOperationException("User is not authenticated."))
                        .Single();

                if (bankUser != null)
                {
                    Console.WriteLine($"Loaded user: {supabaseService.GetClient().Auth.CurrentSession?.User?.Email} Authority: {bankUser.AuthorityLevel}");
                    return bankUser.AuthorityLevel;
                }
                else
                {
                    Console.WriteLine("Failed to load user authority level.");
                }
                return _userService.authoritylevel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Authority level: {ex.Message}");
                throw;
            }
        }


        /*public async Task<string> FetchName()
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
        }*/

        public async Task<bool> AddToBalance(float amountToAdd)
        {
            var _supabaseClient = _supabaseService.GetClient();

            try
            {
                var userId = _supabaseClient.Auth.CurrentSession?.User?.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User is not authenticated. - Services.AddToBalance");
                    return false;
                }
                Console.WriteLine($"Supabase Auth ID: {userId} - Services.AddToBalance");
                var response = await _supabaseClient
                    .From<BankUser>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Get();
                if (response.Models == null || !response.Models.Any())
                {
                    Console.WriteLine("No user model found for given ID. - Services.AddToBalance");
                    return false;
                }
                var user = response.Models.First();
                Console.WriteLine($"Current balance: {user.Balance} - Services.AddToBalance");
                user.Balance += (decimal)amountToAdd;

                var updateResponse = await _supabaseClient
                    .From<BankUser>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Update(user);

                if (updateResponse != null)
                {
                    if (updateResponse.Models.Count > 0)
                    {
                        Console.WriteLine("Balance update successful. - Services.AddToBalance");
                        _userService.balance = (float)user.Balance;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Update response empty. - Services.AddToBalance");
                        return false;
                    }

                }
                else
                {
                    Console.WriteLine("Balance update failed - update response null. - Services.AddToBalance");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating balance: {ex.Message} - Services.AddToBalance");
                return false;
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

                BankUser bankUser = await _supabaseService.GetClient()
                        .From<BankUser>()
                        .Select("name")
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, _supabaseService.GetClient().Auth.CurrentSession?.User?.Id ?? throw new InvalidOperationException("User is not authenticated."))
                        .Single();

                if (bankUser != null)
                {
                    Console.WriteLine($"Loaded user: {_supabaseService.GetClient().Auth.CurrentSession?.User?.Email} Name: {bankUser.Name}");
                    return bankUser.Name;
                }
                else
                {
                    Console.WriteLine("Failed to load user name.");
                }
                return _userService.name ?? "Name not found";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching name: {ex.Message}");
                throw;
            }
        } 

        
    }
}