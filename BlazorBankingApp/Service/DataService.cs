/*
    This file is an extension of the Services.cs file.
    It contains more functions to manage the data in the database.
    Allows us to easily reuse the code and keep it clean.
*/

using Supabase;
using BlazorBankingApp.Services;
using System;
using System.Threading.Tasks;



namespace BlazorBankingApp.Service
{

    // This class is used to manage the data in the database.
    // It contains functions to fetch the balance, authority level, and name of the user.
    // It also contains functions to add to the balance and update the user information.
    // It uses the Supabase client to connect to the database and perform the operations.
    public class DataService
    {
        // The Supabase client is used to connect to the database and perform the operations.
        private readonly SupabaseService _supabaseService;

        // The UserService is used to manage the user information.
        private readonly UserService _userService;


        /// <summary>
        /// Initializes a new instance of the <see cref="DataService"/> class.
        /// This constructor sets up the necessary services for data management operations.
        /// </summary>
        /// <param name="supabaseService">The service used for Supabase client operations and database management.</param>
        /// <param name="userService">The service used for managing user-related operations and information.</param>
        public DataService(SupabaseService supabaseService, UserService userService)
        {
            _supabaseService = supabaseService;
            _userService = userService;
        }
        public async Task<List<Account>> GetUserAccounts()
        {
            var session = _supabaseService.GetClient().Auth.CurrentSession;
            if (session == null || session.User == null)
                throw new InvalidOperationException("No user is logged in.");

            var userId = Guid.Parse(session.User.Id);

            var response = await _supabaseService.GetClient()
                .From<Account>()
                .Filter("user_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            return response.Models;
        }



        /// <summary>
        /// Retrieves the current user's balance from the BankUser table in the PostgreSQL database.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains the user's balance as a float 
        /// if successful; otherwise, 0f if the user balance could not be found.
        /// </returns>
        /// <remarks>
        /// This method attempts to restore the user's session and fetches the balance for the authenticated user.
        /// If the session or user is unavailable, an exception is thrown. The balance is updated in the UserService
        /// if retrieved successfully. Any errors encountered during the process are logged and rethrown.
        /// </remarks>
        public async Task<float> FetchBalance()
        {
            try
            {
                var session = _supabaseService.GetClient().Auth.CurrentSession;
                if (session == null || session.User == null)
                {
                    throw new InvalidOperationException("User session is not available. Please log in again. - DataService.FetchBalance");
                }
                Console.WriteLine($"Session restored. User ID: {session.User.Id} - DataService.FetchBalance");

                using var supabaseService = _supabaseService;
                await supabaseService.RestoreSession();

                Console.WriteLine($"Fetching balance for user ID: {session.User.Id} - DataService.FetchBalance");

                // Fetch the balance for the authenticated user
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

                Console.WriteLine($"Debug query result count: {debugResponse.Models.Count} - DataService.FetchBalance");

                if (bankUser != null)
                {
                    //_userService.balance = (float)bankUser.Balance;
                    Console.WriteLine($"Loaded user: {supabaseService.GetClient().Auth.CurrentSession?.User?.Email} Balance: {_userService.balance} - DataService.FetchBalance");
                    return _userService.balance;
                }
                else
                {
                    Console.WriteLine("Failed to load user balance. - DataService.FetchBalance");
                }
                return 0f; //means users balance was not found
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching balance: {ex.Message} - {ex.StackTrace ?? "DataService.FetchBalance"}");
                throw;
            }
        }

        public async Task<decimal> FetchBalanceForAccount(Guid accountId)
        {
            var response = await _supabaseService.GetClient()
                .From<Account>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, accountId.ToString())
                .Single();

            return response.Balance;
        }





        /// <summary>
        /// Retrieves the authority level of the current user from the BankUser table in the PostgreSQL database.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation. The task result contains the user's authority level as a string
        /// if successful; otherwise, it returns the existing authority level from the <see cref="UserService"/> if the user data could not be loaded.
        /// </returns>
        /// <remarks>
        /// This method attempts to restore the user's session and fetches the authority level for the authenticated user.
        /// If the session or user is unavailable, an exception is thrown. The authority level is logged to the console if retrieved successfully.
        /// Any errors encountered during the process are logged and rethrown.
        /// </remarks>
        public async Task<string> FetchAuthority()
        {
            try
            {
                var session = _supabaseService.GetClient().Auth.CurrentSession;
                if (session == null)
                {
                    throw new InvalidOperationException("User session is not available. Please log in again.");
                }


                // Fetch the authority level for the authenticated user
                using var supabaseService = new SupabaseService();
                await supabaseService.RestoreSession();
                BankUser bankUser = await supabaseService.GetClient()
                        .From<BankUser>()
                        .Select("authoritylevel")
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, supabaseService.GetClient().Auth.CurrentSession?.User?.Id ?? throw new InvalidOperationException("User is not authenticated."))
                        .Single();

                //If the count == 0 then the user is not in the BankUser table
                if (bankUser != null)
                {
                    Console.WriteLine($"Loaded user: {supabaseService.GetClient().Auth.CurrentSession?.User?.Email} Authority: {bankUser.AuthorityLevel} - DataService.FetchAuthority");
                    return bankUser.AuthorityLevel;
                }
                else
                {
                    Console.WriteLine("Failed to load user authority level. - DataService.FetchAuthority");
                }
                return _userService.authoritylevel;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching Authority level: {ex.Message} - {ex.StackTrace ?? "DataService.FetchAuthority"}");
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


        /// <summary>
        /// Adds a given amount to the user's current balance in the BankUser table.
        /// </summary>
        /// <param name="amountToAdd">The amount to add to the user's balance.</param>
        /// <returns>
        /// <c>true</c> if the balance update was successful, <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// This method first checks if the user is authenticated. If not, it returns false.
        /// Then, it fetches the user's current balance from the BankUser table and adds the given amount to it.
        /// The updated balance is then written back to the BankUser table.
        /// If the update is successful, the method returns <c>true</c>. If the update fails or the user is not authenticated,
        /// the method returns <c>false</c>. Any errors encountered during the process are logged and rethrown.
        /// </remarks>
        public async Task<bool> AddToBalance(float amountToAdd)
        {
            var _supabaseClient = _supabaseService.GetClient();

            try
            {
                // Get the current user ID from the Supabase client
                var userId = _supabaseClient.Auth.CurrentSession?.User?.Id;
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User is not authenticated. - DataService.AddToBalance");
                    return false;
                }

                // Fetch the current balance for the user
                Console.WriteLine($"Supabase Auth ID: {userId} - DataService.AddToBalance");
                var response = await _supabaseClient
                    .From<BankUser>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Get();

                // Check if the response contains any models
                if (response.Models == null || !response.Models.Any())
                {
                    Console.WriteLine("No user model found for given ID. - DataService.AddToBalance");
                    return false;
                }

                // Get the first model from the response
                var user = response.Models.First();
                Console.WriteLine($"Current balance: {user.Balance} - DataService.AddToBalance");

                // Add the amount to the user's balance
                user.Balance += (decimal)amountToAdd;

                // Update the user's balance in the BankUser table
                var updateResponse = await _supabaseClient
                    .From<BankUser>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, userId)
                    .Update(user);

                // Check if the update was successful
                // If the update response is not null and contains models, the update was successful
                if (updateResponse != null)
                {
                    if (updateResponse.Models.Count > 0)
                    {
                        Console.WriteLine("Balance update successful. - DataService.AddToBalance");
                        _userService.balance = (float)user.Balance;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Update response empty. - DataService.AddToBalance");
                        return false;
                    }

                }
                else
                {
                    Console.WriteLine("Balance update failed - update response null. - DataService.AddToBalance");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating balance: {ex.Message} - DataService.AddToBalance");
                return false;
            }
        }


        /// <summary>
        /// Fetches the name of the currently authenticated user from the BankUser table.
        /// </summary>
        /// <returns>
        /// The name of the currently authenticated user, or "Name not found" if the user is not authenticated or the name cannot be fetched.
        /// </returns>
        /// <remarks>
        /// This method first checks if the user is authenticated. If not, it throws an InvalidOperationException.
        /// Then, it fetches the user's name from the BankUser table and returns it.
        /// If the fetch fails, the method returns "Name not found".
        /// Any errors encountered during the process are logged and rethrown.
        /// </remarks>
        public async Task<string> FetchName()
        {
            try
            {
                var session = _supabaseService.GetClient().Auth.CurrentSession;
                if (session == null)
                {
                    throw new InvalidOperationException("User session is not available. Please log in again. - DataService.FetchName");
                }

                // Fetch the name for the authenticated user
                BankUser bankUser = await _supabaseService.GetClient()
                        .From<BankUser>()
                        .Select("name")
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, _supabaseService.GetClient().Auth.CurrentSession?.User?.Id ?? throw new InvalidOperationException("User is not authenticated."))
                        .Single();

                //If the count == 0 then the user is not in the BankUser table
                if (bankUser != null)
                {
                    Console.WriteLine($"Loaded user: {_supabaseService.GetClient().Auth.CurrentSession?.User?.Email} Name: {bankUser.Name} - DataService.FetchName");
                    return bankUser.Name;
                }
                else
                {
                    Console.WriteLine("Failed to load user name. - DataService.FetchName");
                }
                return _userService.name ?? "Name not found - DataService.FetchName";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching name: {ex.Message} - {ex.StackTrace ?? "DataService.FetchName"}");
                throw;
            }
        }
    }
}