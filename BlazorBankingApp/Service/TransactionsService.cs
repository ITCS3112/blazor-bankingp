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

        // 4. Log transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            ReceiverId = receiverId,
            Amount = amount,
            Timestamp = DateTime.UtcNow,
        };

        // 5. Send updates (Supabase doesn't support transactions natively — do these sequentially)
        try
        {
            await _supabase.From<BankUser>().Update(sender);
            await _supabase.From<BankUser>().Update(receiver);
            Console.WriteLine($"Logging transaction: {transaction.Amount} from {transaction.SenderId} to {transaction.ReceiverId}");
            Console.WriteLine($"Inserting transaction: {transaction.Id}, {transaction.SenderId}, {transaction.ReceiverId}, {transaction.Amount}, {transaction.Timestamp}");

            // Log the transaction

            var insertResult = await _supabase.From<Transaction>().Insert(transaction);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during inserResult: {ex.Message}");
        }

        //Testing transfer
        try
        {
            var directInsert = await _supabase.From<Transaction>().Insert(new Transaction
            {
                Id = Guid.NewGuid(),
                SenderId = Guid.NewGuid(),
                ReceiverId = Guid.NewGuid(),
                Amount = 5.55M,
                Timestamp = DateTime.UtcNow
            });
            Console.WriteLine($"Inserted transaction? Count: {directInsert.Models.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during direct insert: {ex.Message}");
        }
        return "Transaction completed successfully.";
    }
}
