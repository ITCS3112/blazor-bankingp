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
        var currentAccount = _userService.CurrentAccount;

        if (currentAccount == null)
        {
            Console.WriteLine("No current account is set. - DataService.AddToBalance");
            return false;
        }

        Console.WriteLine($"Updating balance for Account ID: {currentAccount.Id}");

        // Add to balance
        currentAccount.Balance += (decimal)amountToAdd;

        // Push the updated balance to Supabase
        var updateResponse = await _supabaseClient
            .From<Account>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, currentAccount.Id.ToString())
            .Update(currentAccount);

        if (updateResponse != null && updateResponse.Models.Any())
        {
            Console.WriteLine("Balance update successful. - DataService.AddToBalance");

            // Sync local balance for UI/logic
            _userService.balance = (float)currentAccount.Balance;
            return true;
        }
        else
        {
            Console.WriteLine("Update failed or returned no updated models. - DataService.AddToBalance");
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