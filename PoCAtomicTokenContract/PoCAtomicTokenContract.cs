using System;
using System.Numerics;
using System.ComponentModel;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;


namespace PoCAtomicTokenContract
{
	public class PoCAtomicTokenContract : SmartContract
	{
		#region Variables.
	
		/// <summary>
		/// Name of token.
		/// </summary>
		/// <returns></returns>
		public static string Name() => "AtomicSwapToken";

		/// <summary>
		/// Symbol of token.
		/// </summary>
		/// <returns></returns>
		public static string Symbol() => "AST";

		/// <summary>
		/// Decimals of token.
		/// </summary>
		/// <returns></returns>
		public static byte Decimals() => 8;

		/// <summary>
		/// Event for notify about transferred tokens.
		/// </summary>
		[DisplayName("transfer")]
		public static event Action<byte[], byte[], BigInteger> Transferred;

		/// <summary>
		/// Event for notify about refund tokens.
		/// </summary>
		[DisplayName("refund")]
		public static event Action<byte[], BigInteger> Refund;

		/// <summary>
		/// Event for notify about approved.
		/// </summary>
		[DisplayName("approve")]
		public static event Action<byte[], byte[], BigInteger> Approved;

		/// <summary>
		/// Event for notify about redeem tokens.
		/// </summary>
		[DisplayName("redeem")]
		public static event Action<byte[], byte[], BigInteger> Redeem;

		#endregion

		#region Main function.

		/// <summary>
		/// Main function for this contract.
		/// </summary>
		/// <param name="operation">Operation for invoke.</param>
		/// <param name="args">Arg list.</param>
		/// <returns>Result of invoke.</returns>
		public static Object Main(String operation, params Object[] args)
		{
			Runtime.Notify("Invoke AtomicTokenContract.", operation, args);

			try
			{
				if (operation == "balanceOf")
					return Storage.Get(Storage.CurrentContext, (byte[])args[0]).AsBigInteger();
				else if (operation == "totalSupply")
					return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
				else if (operation == "name")
					return Name();
				else if (operation == "symbol")
					return Symbol();
				else if (operation == "decimals")
					return Decimals();
				else if (operation == "Deploy")
				{
					// Check args length.
					if (args.Length != 5)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get all variables from args list.
					byte[] ownerForDeploy = (byte[])args[0];
					byte[] participant = (byte[])args[1];
					BigInteger amount = ((byte[])args[2]).AsBigInteger();
					byte[] secretHashForDeploy = (byte[])args[3];
					uint timeout = (uint)((byte[])args[4]).AsBigInteger();

					return Deploy(ownerForDeploy, participant, amount, secretHashForDeploy, timeout);
				}
				else if (operation == "Redeem")
				{
					// Check arg length.
					if (args.Length != 2)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get all variables from arg list.
					byte[] ownerForRedeem = (byte[])args[0];
					byte[] secret = (byte[])args[1];

					// Compute secret hash from secret word.
					byte[] secretHashForRedeem = Sha256(secret);

					return RedeemTokens(ownerForRedeem, secretHashForRedeem);
				}
				else if (operation == "Refund")
				{
					// Check args length.
					if (args.Length != 1)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get variable from arg list.
					byte[] ownerForRefund = (byte[])args[0];

					return RefundTokens(ownerForRefund);
				}
				else if (operation == "MintTokens")
				{
					// Check args length.
					if (args.Length != 3)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get all variables from arg list.
					byte[] ownerForMint = (byte[])args[0];
					byte[] to = (byte[])args[1];
					BigInteger amountForMint = ((byte[])args[2]).AsBigInteger();

					return MintTokens(ownerForMint, to, amountForMint);
				}
				else if (operation == "Transfer")
				{
					// Check args length.
					if (args.Length != 3)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get all variables from arg list.
					byte[] fromForTransfer = (byte[])args[0];
					byte[] toForTransfer = (byte[])args[1];
					BigInteger amount = ((byte[])args[2]).AsBigInteger();

					return Transfer(fromForTransfer, toForTransfer, amount);
				}
				else if (operation == "Allowance")
				{
					// Check args length.
					if (args.Length != 2)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get all variables from arg list.
					byte[] fromForAllowance = (byte[])args[0];
					byte[] toForAllowance = (byte[])args[1];

					return Allowance(fromForAllowance, toForAllowance);
				}
				else if (operation == "TransferFrom")
				{
					// Check args length.
					if (args.Length != 4)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get all variables from arg list.
					byte[] originatorForTransferFrom = (byte[])args[0];
					byte[] fromForTransferFrom = (byte[])args[1];
					byte[] toForTransferFrom = (byte[])args[2];
					BigInteger amountForTransferFrom = ((byte[])args[3]).AsBigInteger();

					return TransferFrom(originatorForTransferFrom, fromForTransferFrom, toForTransferFrom, amountForTransferFrom);
				}
				else if (operation == "Approve")
				{
					// Check args length.
					if (args.Length != 3)
					{
						Runtime.Log("Args not valid.");
						return false;
					}

					// Get all variables from arg list.
					byte[] originatorForApprove = (byte[])args[0];
					byte[] ownerForApprove = (byte[])args[1];
					BigInteger amountForApprove = ((byte[])args[2]).AsBigInteger();

					return Approve(originatorForApprove, ownerForApprove, amountForApprove);
				}
				else
				{
					Runtime.Log("Unknown operation");
					return false;
				}
			}
			catch (Exception e)
			{
				Runtime.Notify("Error.", e);
				return false;
			}
		}

		#endregion

		#region Methods for token logic.

		/// <summary>
		///   Transfers freezy tokens from temp storage to account by originator.
		/// </summary>
		/// <param name="from">
		///   The account to transfer a balance from.
		/// </param>
		/// <param name="to">
		///   The account to transfer a balance to.
		/// </param>
		/// <param name="amount">
		///   The amount to transfer.
		/// </param>
		/// <returns>
		///   Transaction successful?
		/// </returns>
		private static Boolean TransferFrom(Byte[] originator, Byte[] from, Byte[] to, BigInteger amount)
		{
			// Verifiaction originator.
			if (!Runtime.CheckWitness(originator))
			{
				Runtime.Notify("Invoker not originator.");
				return false;
			}

			if (Allowance(originator, from))
			{
				// Load the information for the requested transaction from field storage.
				BigInteger balance = Storage.Get(Storage.CurrentContext, originator.Concat(from)).AsBigInteger();

				// Check valid request.
				if (balance < amount)
				{
					Runtime.Notify("Balance < amount");
					return false;
				}

				// Give tokens toAccount value.
				BigInteger toAmount = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
				Storage.Put(Storage.CurrentContext, to, toAmount + amount);

				// Take tokens from or delete temporary storage.
				if (balance == amount)
					Storage.Delete(Storage.CurrentContext, originator.Concat(from));
				else
					Storage.Put(Storage.CurrentContext, originator.Concat(from), balance - amount);

				Runtime.Notify("TransferFrom success.");
				Transferred(from, to, balance);
				return true;
			}

			Runtime.Notify("Transfer failed.", originator, from, to);
			return false;
		}

		/// <summary>
		/// Transfer amount from one account to another.
		/// </summary>
		/// <param name="from">
		///   The account to transfer a balance from.
		/// </param>
		/// <param name="to">
		///   The account to transfer a balance to.
		/// </param>
		/// <param name="amount">
		///   The amount to transfer.
		/// </param>
		/// <returns>
		///   Transaction successful?
		/// </returns>
		private static Boolean Transfer(Byte[] from, Byte[] to, BigInteger amount)
		{
			#region Check valid request.

			// 'From' verification.
			if (!Runtime.CheckWitness(from))
			{
				Runtime.Notify("Invoker not owner.");
				return false;
			}

			// Amount valid check.
			BigInteger fromAmount = Storage.Get(Storage.CurrentContext, from).AsBigInteger();

			if (fromAmount < amount)
			{
				Runtime.Notify("Not funds.", from, fromAmount);
				return false;
			}

			#endregion

			#region Transfer.

			// Take tokens from 'From'.
			if (fromAmount == amount)
				Storage.Delete(Storage.CurrentContext, from);
			else
				Storage.Put(Storage.CurrentContext, from, fromAmount - amount);

			// Give tokens to 'To',
			var toAmount = Storage.Get(Storage.CurrentContext, to);

			if (toAmount.Length != 0)
				Storage.Put(Storage.CurrentContext, to, toAmount.AsBigInteger() + amount);
			else
				Storage.Put(Storage.CurrentContext, to, amount);

			Runtime.Notify("Transfer success");
			Transferred(from, to, amount);

			return true;

			#endregion
		}

		/// <summary>
		/// Only owner can call this.
		/// This function only for DEBUG and needn't.
		/// All tokens mint by PoC contract.
		/// </summary>
		/// <returns></returns>
		private static Boolean MintTokens(Byte[] owner, Byte[] to, BigInteger amount)
		{
			#region Check valid request.

			// Owner verification.
			if (!Runtime.CheckWitness(owner))
			{
				Runtime.Notify("Invoker not owner.");
				return false;
			}

			#endregion

			#region Mint tokens.

			// Total supply change.
			var totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply");

			if (totalSupply.Length != 0)
				Storage.Put(Storage.CurrentContext, "totalSupply", totalSupply.AsBigInteger() + amount);
			else
				Storage.Put(Storage.CurrentContext, "totalSupply", amount);


			// Give tokens.
			var toAcc = Storage.Get(Storage.CurrentContext, to);

			if (toAcc.Length != 0)
				Storage.Put(Storage.CurrentContext, to, toAcc.AsBigInteger() + amount);
			else
				Storage.Put(Storage.CurrentContext, to, amount);

			Runtime.Notify("MintTokens success.", to, toAcc.AsBigInteger() + amount, totalSupply.AsBigInteger() + amount);
			Transferred(null, to, amount);

			return true;

			#endregion
		}

		/// <summary>
		///   Approves another user to use the TransferFrom function on the invoker's account.
		/// </summary>
		/// <param name="originator">
		///   The contract invoker.
		/// </param>
		/// <param name="to">
		///   The account to grant TransferFrom access to.
		/// </param>
		/// <param name="amount">
		///   The amount to grant TransferFrom access for.
		/// </param>
		/// <returns>
		///   Transaction Successful?
		/// </returns>
		private static Boolean Approve(Byte[] originator, Byte[] owner, BigInteger amount)
		{
			#region Check correctly

			// Owner verification.
			if (!Runtime.CheckWitness(owner))
			{
				Runtime.Notify("Invoker not owner.");
				return false;
			}

			// Amount valid check.
			var ownerBalance = Storage.Get(Storage.CurrentContext, owner).AsBigInteger();

			if (ownerBalance < amount)
			{
				Runtime.Notify("OwnerBalance < amount");
				return false;
			}

			if (amount <= 0)
			{
				Runtime.Notify("Amount <= 0");
				return false;
			}

			#endregion

			#region Approve

			// Take tokens from owner.
			if (ownerBalance == amount)
				Storage.Delete(Storage.CurrentContext, owner);
			else
				Storage.Put(Storage.CurrentContext, owner, ownerBalance - amount);

			// Create temp storage and storage for tokens.
			var tempStorage = Storage.Get(Storage.CurrentContext, originator.Concat(owner));

			if (tempStorage.Length != 0)
				Storage.Put(Storage.CurrentContext, originator.Concat(owner), tempStorage.AsBigInteger() + amount);
			else
				Storage.Put(Storage.CurrentContext, originator.Concat(owner), amount);

			Runtime.Notify("Approve success.");
			Approved(originator, owner, amount);
			return true;

			#endregion
		}

		/// <summary>
		///   Checks approval of two accounts.
		/// </summary>
		/// <param name="from">
		///   The account which funds can be transfered from.
		/// </param>
		/// <param name="to">
		///   The account which is granted usage of the account.
		/// </param>
		/// <returns>
		///   Exist or not exist.
		/// </returns>
		private static Boolean Allowance(Byte[] from, Byte[] to)
		{
			var balance = Storage.Get(Storage.CurrentContext, from.Concat(to));

			if (balance.Length != 0)
			{
				Runtime.Notify("Allowance success.", from.Concat(to), balance);
				return true;
			}

			Runtime.Notify("Not exists.", from.Concat(to));
			return false;
		}

		#endregion

		#region Methods for atomic swap.

		/// <summary>
		///   Deploy atomic swap.
		/// </summary>
		/// <param name="owner">
		///   The account which deploy swap.
		/// </param>
		/// <param name="participant">
		///   The account which participant in atomic swap.
		/// </param>
		/// <param name="amount">
		///   The amount to atomic swap.
		/// </param>
		/// <param name="secretHash">
		///   The secret hash for redeem.
		/// </param>
		/// <param name="timeout">
		///   Timeout in seconds for atomic swap life time.
		/// </param>
		/// <returns>
		///   Deploy successful?
		/// </returns>
		private static Boolean Deploy(Byte[] owner, Byte[] participant, BigInteger amount, Byte[] secretHash, uint timeout)
		{
			#region Check valid request.

			// Owner verification.
			if (!Runtime.CheckWitness(owner))
			{
				Runtime.Notify("Invoker not owner.");
				return false;
			}

			// Verification of existence.
			if (Allowance(owner, participant))
			{
				Runtime.Log("Contract already deploy.");
				return false;
			}

			// Check valid SecretHash.
			if (secretHash.Length != 32)
			{
				Runtime.Notify("SecretHash.Length != 32", secretHash, secretHash.Length);
				return false;
			}

			// Check amount correctly.
			if (amount <= 0)
			{
				Runtime.Notify("Amount <= 0", amount);
				return false;
			}

			#endregion

			#region Deploy swap.

			if (Approve(ExecutionEngine.ExecutingScriptHash, owner, amount))
			{
				uint dateOfTime = Runtime.Time + timeout;

				byte[] storageKey = owner.Concat("PoC".AsByteArray());
				Storage.Put(Storage.CurrentContext, storageKey.Concat("Participant".AsByteArray()), participant);
				Storage.Put(Storage.CurrentContext, storageKey.Concat("Amount".AsByteArray()), amount.AsByteArray());
				Storage.Put(Storage.CurrentContext, storageKey.Concat("DateOfTime".AsByteArray()), dateOfTime);
				Storage.Put(Storage.CurrentContext, storageKey.Concat("Secret".AsByteArray()), secretHash);

				Runtime.Notify("Deploy complete.", owner, participant, amount, timeout);
				return true;
			}

			Runtime.Notify("Deploy failed.", owner, participant, amount, timeout);
			return false;

			#endregion
		}

		/// <summary>
		///   Redeem tokens from atomic swap.
		/// </summary>
		/// <param name="owner">
		///   The account which deploy swap.
		/// </param>
		/// <param name="secretHash">
		///   The secret hash for redeem tokens.
		/// </param>
		/// <returns>
		///   Redeem successful?
		/// </returns>
		private static Boolean RedeemTokens(Byte[] owner, Byte[] secretHash)
		{
			#region Check valid request.

			// Check secret hash valid and correctly.
			byte[] storageKey = owner.Concat("PoC".AsByteArray());
			byte[] curSecretHash = Storage.Get(Storage.CurrentContext, storageKey.Concat("Secret".AsByteArray()));

			if (secretHash != curSecretHash)
			{
				Runtime.Notify("Your secret incorrect.", secretHash);
				return false;
			}

			// DeployDate conformity check.
			uint dateOfTime = (uint)Storage.Get(Storage.CurrentContext, storageKey.Concat("DateOfTime".AsByteArray())).AsBigInteger();
			uint redeemDate = Runtime.Time;

			if (redeemDate >= dateOfTime)
			{
				Runtime.Log("Contract overdue.");
				return false;
			}

			#endregion

			#region Redeem.

			byte[] atomicContract = ExecutionEngine.ExecutingScriptHash;
			byte[] participant = Storage.Get(Storage.CurrentContext, storageKey.Concat("Participant".AsByteArray()));
			BigInteger amount = Storage.Get(Storage.CurrentContext, storageKey.Concat("Amount".AsByteArray())).AsBigInteger();

			if (Allowance(atomicContract, owner))
			{
				// Load the information for the requested transaction from field storage.
				BigInteger balance = Storage.Get(Storage.CurrentContext, atomicContract.Concat(owner)).AsBigInteger();

				// Check valid request.
				if (balance < amount)
				{
					Runtime.Notify("Balance < amount");
					return false;
				}

				// Give tokens toAccount value.
				BigInteger toAmount = Storage.Get(Storage.CurrentContext, participant).AsBigInteger();
				Storage.Put(Storage.CurrentContext, participant, toAmount + amount);

				// Take tokens from or delete temporary storage.
				if (balance == amount)
					Storage.Delete(Storage.CurrentContext, atomicContract.Concat(owner));
				else
					Storage.Put(Storage.CurrentContext, atomicContract.Concat(owner), balance - amount);

				// Delete all temp storage data.
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("Participant".AsByteArray()));
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("Amount".AsByteArray()));
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("DateOfTime".AsByteArray()));
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("Secret".AsByteArray()));

				Runtime.Notify("Redeem success.");
				Redeem(owner, participant, amount);
				return true;
			}

			Runtime.Notify("Redeem failed.", owner, participant);
			return false;

			#endregion
		}

		/// <summary>
		///   Refund tokens from atomic swap.
		/// </summary>
		/// <param name="owner">
		///   The account which deploy swap.
		/// </param>
		/// <returns>
		///   Refund successful?
		/// </returns>
		private static Boolean RefundTokens(Byte[] owner)
		{
			#region Check valid request.

			// Verification owner.
			if (!Runtime.CheckWitness(owner))
			{
				Runtime.Notify("Invoker not owner.");
				return false;
			}

			if (!Allowance(ExecutionEngine.ExecutingScriptHash, owner))
			{
				Runtime.Notify("Refund failed.", owner);
				return false;
			}

			// DeployDate conformity check.
			byte[] storageKey = owner.Concat("PoC".AsByteArray());
			var timeout = Storage.Get(Storage.CurrentContext, storageKey.Concat("Timeout".AsByteArray()));
			uint redeemDate = Runtime.Time;

			if (redeemDate < (uint)timeout.AsBigInteger())
			{
				Runtime.Log("Contract not overdue.");
				return false;
			}

			#endregion

			#region Refund tokens.

			byte[] atomicContract = ExecutionEngine.ExecutingScriptHash;
			BigInteger amount = Storage.Get(Storage.CurrentContext, owner.Concat("Amount".AsByteArray())).AsBigInteger();

			if (Allowance(atomicContract, owner))
			{
				// Load the information for the requested transaction from field storage.
				BigInteger balance = Storage.Get(Storage.CurrentContext, atomicContract.Concat(owner)).AsBigInteger();

				// Check valid request.
				if (balance < amount)
				{
					Runtime.Notify("Balance < amount");
					return false;
				}

				// Give tokens toAccount value.
				BigInteger toAmount = Storage.Get(Storage.CurrentContext, owner).AsBigInteger();
				Storage.Put(Storage.CurrentContext, owner, toAmount + amount);

				// Take tokens from or delete temporary storage.
				if (balance == amount)
					Storage.Delete(Storage.CurrentContext, atomicContract.Concat(owner));
				else
					Storage.Put(Storage.CurrentContext, atomicContract.Concat(owner), balance - amount);

				// Delete all temp storage data.
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("Participant".AsByteArray()));
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("Amount".AsByteArray()));
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("DateOfTime".AsByteArray()));
				Storage.Delete(Storage.CurrentContext, storageKey.Concat("Secret".AsByteArray()));

				Runtime.Notify("Refund success.");
				Refund(owner, amount);
				return true;
			}

			Runtime.Notify("Refund failed.", owner);
			return false;

			#endregion
		}

		#endregion
	}
}
