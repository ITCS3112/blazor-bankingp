using System.ComponentModel.DataAnnotations.Schema;
public class TransactionsService
{
    private readonly Supabase.Client _supabase;


    public TransactionsService(Supabase.Client supabaseClient)
    {
        _supabase = supabaseClient;
    }


    public async Task<string> SendTransactionAsync(Guid senderId, Guid receiverId, decimal amount)
    {
        if (amount <= 0)
            return "Amount must be greater than zero.";


        if (senderId == receiverId)
            return "Cannot send money to yourself.";


        // 1. Get both users
        var senderResp = await _supabase
        .From<BankUser>()
        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, senderId.ToString())
        .Get();


        var receiverResp = await _supabase
            .From<BankUser>()
            .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, receiverId.ToString())
            .Get();


        var sender = senderResp.Models.FirstOrDefault();
        var receiver = receiverResp.Models.FirstOrDefault();


        if (sender == null || receiver == null)
            return "Sender or receiver not found.";


        // 2. Check balance
        if (sender.Balance < amount)
            return "Insufficient funds.";


        // 3. Update balances
        sender.Balance -= amount;
        receiver.Balance += amount;


        try
        {
            await _supabase.From<BankUser>().Update(sender);
            await _supabase.From<BankUser>().Update(receiver);


            var transactionId = Guid.NewGuid();
            var logTime = DateTime.UtcNow;


            await _supabase.Rpc("log_transaction", new
            {
                transaction_id = transactionId,
                sender_id = senderId,
                receiver_id = receiverId,
                amount = amount,
                log_time = logTime


            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during transaction: {ex.Message}");
        }


        return "Transaction successful!";
    }


    public async Task<List<Transaction>> GetTransactionsAsync(Guid userId)
{
    try
    {
        // Get transactions where user is the sender
        var senderTransactions = await _supabase
            .From<Transaction>()
            .Filter("sender_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
            .Get();
            
        // Get transactions where user is the receiver
        var receiverTransactions = await _supabase
            .From<Transaction>()
            .Filter("receiver_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
            .Get();
            
        // Combine the results
        var combined = new List<Transaction>();
        combined.AddRange(senderTransactions.Models);
        combined.AddRange(receiverTransactions.Models);
        
        // Sort by timestamp (most recent first)
        var orderedTransactions = combined.OrderByDescending(t => t.Timestamp).ToList();
        
        // Collect all unique user IDs that we need to look up
        var userIds = new HashSet<Guid>();
        foreach (var transaction in orderedTransactions)
        {
            userIds.Add(transaction.SenderId);
            userIds.Add(transaction.ReceiverId);
        }
        
        // Create a dictionary to store user names
        var userNames = new Dictionary<Guid, string>();
        
        // Fetch user names for each unique ID
        foreach (var id in userIds)
        {
            try
            {
                var userResponse = await _supabase
                    .From<BankUser>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();
                
                var user = userResponse.Models.FirstOrDefault();
                if (user != null && !string.IsNullOrEmpty(user.Name))
                {
                    userNames[id] = user.Name;
                }
                else
                {
                    userNames[id] = $"User {id.ToString().Substring(0, 8)}";
                }
            }
            catch
            {
                userNames[id] = $"User {id.ToString().Substring(0, 8)}";
            }
        }
        
        // Add name properties to each transaction
        foreach (var transaction in orderedTransactions)
        {
            transaction.SenderName = userNames.ContainsKey(transaction.SenderId) 
                ? userNames[transaction.SenderId] 
                : $"User {transaction.SenderId.ToString().Substring(0, 8)}";
                
            transaction.ReceiverName = userNames.ContainsKey(transaction.ReceiverId)
                ? userNames[transaction.ReceiverId]
                : $"User {transaction.ReceiverId.ToString().Substring(0, 8)}";
        }
        
        return orderedTransactions;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching transactions: {ex.Message}");
        return new List<Transaction>();
    }
}
}
