using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using SolanaRoyale;
using SolanaRoyale.Program;
using SolanaRoyale.Errors;
using SolanaRoyale.Accounts;

namespace SolanaRoyale
{
    namespace Accounts
    {
        public partial class PlayerScore
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 17418430625039616009UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{9, 136, 69, 222, 93, 178, 186, 241};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "2bUaGbh4CXa";
            public PublicKey PlayerName { get; set; }

            public ulong Score { get; set; }

            public static PlayerScore Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                PlayerScore result = new PlayerScore();
                result.PlayerName = _data.GetPubKey(offset);
                offset += 32;
                result.Score = _data.GetU64(offset);
                offset += 8;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum SolanaRoyaleErrorKind : uint
        {
        }
    }

    public partial class SolanaRoyaleClient : TransactionalBaseClient<SolanaRoyaleErrorKind>
    {
        public SolanaRoyaleClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerScore>>> GetPlayerScoresAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = PlayerScore.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerScore>>(res);
            List<PlayerScore> resultingAccounts = new List<PlayerScore>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => PlayerScore.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerScore>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<PlayerScore>> GetPlayerScoreAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerScore>(res);
            var resultingAccount = PlayerScore.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerScore>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribePlayerScoreAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, PlayerScore> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                PlayerScore parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = PlayerScore.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializePlayerAsync(InitializePlayerAccounts accounts, PublicKey playerName, ulong score, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SolanaRoyaleProgram.InitializePlayer(accounts, playerName, score, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpgradeAsync(UpgradeAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SolanaRoyaleProgram.Upgrade(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendSetScoreAsync(SetScoreAccounts accounts, ulong newScore, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.SolanaRoyaleProgram.SetScore(accounts, newScore, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<SolanaRoyaleErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<SolanaRoyaleErrorKind>>{};
        }
    }

    namespace Program
    {
        public class InitializePlayerAccounts
        {
            public PublicKey PlayerScore { get; set; }

            public PublicKey User { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class UpgradeAccounts
        {
            public PublicKey PlayerScore { get; set; }
        }

        public class SetScoreAccounts
        {
            public PublicKey PlayerScore { get; set; }
        }

        public static class SolanaRoyaleProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializePlayer(InitializePlayerAccounts accounts, PublicKey playerName, ulong score, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerScore, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9239203753139697999UL, offset);
                offset += 8;
                _data.WritePubKey(playerName, offset);
                offset += 32;
                _data.WriteU64(score, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Upgrade(UpgradeAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerScore, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1920037355368607471UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction SetScore(SetScoreAccounts accounts, ulong newScore, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerScore, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(6271472283707615194UL, offset);
                offset += 8;
                _data.WriteU64(newScore, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}