using System.ComponentModel.DataAnnotations.Schema;
public class TransactionsService
{
    private readonly Supabase.Client _supabase;


    public TransactionsService(Supabase.Client supabaseClient)
    {
        _supabase = supabaseClient;
    }


    public async Task<string> SendTransactionAsync(Guid senderAccountId, Guid receiverAccountId, decimal amount, bool isInternal = false)
{
    if (amount <= 0)
        return "Amount must be greater than zero.";

    if (senderAccountId == receiverAccountId)
        return "Cannot send money to the same account.";

    // 1. Get both accounts
    var senderResp = await _supabase
        .From<Account>()
        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, senderAccountId.ToString())
        .Get();

    var receiverResp = await _supabase
        .From<Account>()
        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, receiverAccountId.ToString())
        .Get();

    var sender = senderResp.Models.FirstOrDefault();
    var receiver = receiverResp.Models.FirstOrDefault();

    if (sender == null || receiver == null)
        return "Sender or receiver account not found.";

    // 2. Check balance
    if (sender.Balance < amount)
        return "Insufficient funds.";

    // 3. Update balances
    sender.Balance -= amount;
    receiver.Balance += amount;

    try
    {
        // Save updated balances
        await _supabase.From<Account>().Update(sender);
        await _supabase.From<Account>().Update(receiver);

        // 4. Log transaction (using stored procedure or direct insert)
        var transactionId = Guid.NewGuid();
        var logTime = DateTime.UtcNow;
        await _supabase.Rpc("log_transaction", new
        {
            transaction_id = transactionId,
            sender_id = senderAccountId,
            receiver_id = receiverAccountId,
            amount = amount,
            log_time = logTime,
            is_internal = isInternal
        });

        return $"Transaction successful!";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during transaction: {ex.Message}");
        return "Transaction failed.";
    }
}


    public async Task<List<Transaction>> GetTransactionsAsync(Guid accountId)
{
    try
    {
        // 1. Get transactions where this account is the sender
        var senderTransactions = await _supabase
            .From<Transaction>()
            .Filter("sender_id", Supabase.Postgrest.Constants.Operator.Equals, accountId.ToString())
            .Get();

        // 2. Get transactions where this account is the receiver
        var receiverTransactions = await _supabase
            .From<Transaction>()
            .Filter("receiver_id", Supabase.Postgrest.Constants.Operator.Equals, accountId.ToString())
            .Get();

        // 3. Combine and order
        var combined = new List<Transaction>();
        combined.AddRange(senderTransactions.Models);
        combined.AddRange(receiverTransactions.Models);

        var orderedTransactions = combined
            .OrderByDescending(t => t.Timestamp)
            .ToList();

        // 4. Collect all sender/receiver account IDs
        var accountIds = new HashSet<Guid>();
        foreach (var transaction in orderedTransactions)
        {
            accountIds.Add(transaction.SenderId);
            accountIds.Add(transaction.ReceiverId);
        }

        // 5. Get associated BankUser names for each account
        var userNames = new Dictionary<Guid, string>();
        foreach (var id in accountIds)
        {
            try
            {
                var accountResp = await _supabase
                    .From<Account>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, id.ToString())
                    .Get();

                var account = accountResp.Models.FirstOrDefault();
                if (account != null)
                {
                    // Now get the user associated with this account
                    var userResp = await _supabase
                        .From<BankUser>()
                        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, account.UserId.ToString())
                        .Get();

                    var user = userResp.Models.FirstOrDefault();
                    if (user != null && !string.IsNullOrEmpty(user.Name))
                        userNames[id] = user.Name;
                    else
                        userNames[id] = $"User {id.ToString().Substring(0, 8)}";
                }
            }
            catch
            {
                userNames[id] = $"User {id.ToString().Substring(0, 8)}";
            }
        }

        // 6. Attach user-friendly names
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
    public async Task<List<string>> GetSentEmails(Guid userId)
    {
        var transactions = await _supabase
            .From<Transaction>()
            .Where(t => t.SenderId == userId)
            .Get();

        var recipientIds = transactions.Models.Select(t => t.ReceiverId).Distinct().ToList();

        var recipients = new List<string>();
        foreach (var recipientId in recipientIds)
        {
            var user = await _supabase
        .From<BankUser>()
        .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, recipientId.ToString())
        .Single();
            if (user != null)
                recipients.Add(user.Email);
        }

        return recipients;
    }

    public async Task<List<string>> GetReceivedEmails(Guid userId)
    {
        var transactions = await _supabase
            .From<Transaction>()
            .Where(t => t.ReceiverId == userId)
            .Get();

        var senderIds = transactions.Models.Select(t => t.SenderId).Distinct().ToList();

        var senders = new List<string>();
        foreach (var senderId in senderIds)
        {
            var user = await _supabase
                .From<BankUser>()
                .Filter("id", Supabase.Postgrest.Constants.Operator.Equals, senderId.ToString())
                .Single();
            if (user != null)
                senders.Add(user.Email);
        }

        return senders;
    }
}
