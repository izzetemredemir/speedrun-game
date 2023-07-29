use solana_program::{
    account_info::AccountInfo, pubkey::Pubkey,
    program_error::ProgramError, entrypoint::ProgramResult,
};

use std::collections::HashMap;

solana_program::declare_id!("3sGJfM8VcJt29D1CfrM9tiF7GKvoG3z45P3MGivK2eLc");

use borsh::{BorshSerialize, BorshDeserialize};

// Define the token struct
#[derive(Clone, Debug, PartialEq, BorshSerialize, BorshDeserialize)]
pub struct Token {
    pub name: String,
    pub symbol: String,
    pub total_supply: u64,
    pub decimals: u8,
    pub owner: Pubkey,
    pub balances: HashMap<Pubkey, u64>,
    pub owner_names: HashMap<Pubkey, String>, // Map owner address to their name
}

// Create a new token with a given name, symbol, total supply, and decimals
pub fn create_token(
    accounts: &[AccountInfo],
    name: String,
    symbol: String,
    total_supply: u64,
    decimals: u8,
) -> ProgramResult {
    // Get the accounts
    let token_account = &accounts[0];
    let owner_account = &accounts[1];
    let system_program_account = &accounts[2];
    let rent_account = &accounts[3];

    // Ensure the provided accounts are valid
    if !token_account.is_signer {
        return Err(ProgramError::MissingRequiredSignature);
    }

    // Check if the token account is not already initialized
    if token_account.data.borrow()[0] != 0 {
        return Err(ProgramError::AccountAlreadyInitialized);
    }

    // Token data to store in the token account
    let mut data = Token {
        name,
        symbol,
        total_supply,
        decimals,
        owner: *owner_account.key,
        balances: HashMap::new(),
        owner_names: HashMap::new(),
    };

    // Set the total supply as the balance of the token's owner
    data.balances.insert(*owner_account.key, total_supply);

    // Serialize and store the data in the token account
    data.serialize(&mut &mut token_account.data.borrow_mut()[..])?;

    Ok(())
}

pub fn transfer(
    accounts: &[AccountInfo],
    amount: u64,
    to: Pubkey,
) -> ProgramResult {
    // Get the accounts
    let token_account = &accounts[0];
    let from_account = &accounts[1];
    let to_account = &accounts[2];

    // Ensure the provided accounts are valid
    if !from_account.is_signer {
        return Err(ProgramError::MissingRequiredSignature);
    }

    // Get the token data from the token account
    let mut data = Token::deserialize(&mut &token_account.data.borrow()[..])?;

    // Check if the sender has sufficient balance
    let from_balance_entry = data.balances.get_mut(from_account.key).ok_or(ProgramError::InvalidAccountData)?;
    if *from_balance_entry < amount {
        return Err(ProgramError::Custom(1)); // InsufficientBalance error (custom error code)
    }

    // Check if the "to" account is different from the "from" account
    if from_account.key == to_account.key {
        return Err(ProgramError::InvalidArgument); // Cannot transfer to the same account
    }

    // Perform the transfer
    //let to_balance_entry = data.balances.get_mut(&to).ok_or(ProgramError::InvalidAccountData)?;
    *from_balance_entry -= amount;
    //*to_balance_entry += amount;

    // Serialize and store the updated data back to the token account
    data.serialize(&mut &mut token_account.data.borrow_mut()[..])?;

    Ok(())
}

// Associate a name with the token owner's address
pub fn set_owner_name(
    accounts: &[AccountInfo],
    owner_name: String,
) -> ProgramResult {
    // Get the accounts
    let token_account = &accounts[0];
    let owner_account = &accounts[1];

    // Ensure the provided accounts are valid
    if !owner_account.is_signer {
        return Err(ProgramError::MissingRequiredSignature);
    }

    // Get the token data from the token account
    let mut data = Token::deserialize(&mut &token_account.data.borrow()[..])?;

    // Store the owner's name in the owner_names mapping
    data.owner_names.insert(*owner_account.key, owner_name);

    // Serialize and store the updated data back to the token account
    data.serialize(&mut &mut token_account.data.borrow_mut()[..])?;

    Ok(())
}
