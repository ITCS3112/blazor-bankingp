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
        Console.WriteLine($"GetTransactionsAsync called for user: {userId}");
        try
        {
            var senderTransactions = await _supabase
                .From<Transaction>()
                .Filter("sender_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();


            Console.WriteLine($"Found {senderTransactions.Models.Count} sender transactions");
           
            var receiverTransactions = await _supabase
                .From<Transaction>()
                .Filter("receiver_id", Supabase.Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();
           
            Console.WriteLine($"Found {receiverTransactions.Models.Count} receiver transactions");


            var combined = new List<Transaction>();
            combined.AddRange(senderTransactions.Models);
            combined.AddRange(receiverTransactions.Models);


            foreach (var transaction in combined)
            {
               
            }
       
            return combined.OrderByDescending(t => t.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching transactions in TransactionsService: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return new List<Transaction>();
        }
    }
}
