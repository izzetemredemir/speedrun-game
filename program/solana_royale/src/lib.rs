use solana_program::{
    account_info::{next_account_info, AccountInfo},
    entrypoint,
    entrypoint::ProgramResult,
    msg,
    program_error::ProgramError,
    program_pack::{IsInitialized, Pack, Sealed},
    pubkey::Pubkey,
    sysvar::{rent::Rent, Sysvar},
};

use std::convert::TryInto;

solana_program::declare_id!("6D1ys5CYQy3w8u9SFU6DHZaHdZa2zEBwF9MU3rZAaYvB");

pub struct PlayerScore {
    pub player_name: Pubkey,
    pub score: u64,
}

impl Sealed for PlayerScore {}

impl IsInitialized for PlayerScore {
    fn is_initialized(&self) -> bool {
        self.player_name != Pubkey::default()
    }
}

impl Pack for PlayerScore {
    const LEN: usize = 32 + 8; // size of Pubkey + size of u64

    fn pack_into_slice(&self, dst: &mut [u8]) {
        let src = self.player_name.as_ref();
        dst[..32].copy_from_slice(src);
        let score_bytes = self.score.to_le_bytes();
        dst[32..].copy_from_slice(&score_bytes);
    }

    fn unpack_from_slice(src: &[u8]) -> Result<Self, ProgramError> {
        let player_name = Pubkey::new(&src[..32]);
        let score = u64::from_le_bytes(src[32..].try_into().unwrap());
        Ok(Self { player_name, score })
    }
}

entrypoint!(process_instruction);

pub enum Instruction {
    Upgrade,
    SetScore(u64),
}

pub fn process_instruction(
    program_id: &Pubkey,
    accounts: &[AccountInfo],
    input: &[u8],
) -> ProgramResult {
    let account_info_iter = &mut accounts.iter();
    let account = next_account_info(account_info_iter)?;

    if account.owner != program_id {
        msg!("Account does not have the correct program id");
        return Err(ProgramError::IncorrectProgramId);
    }

    let mut player_score = PlayerScore::unpack_unchecked(&account.data.borrow())?;

    match input[0] {
        0 => {
            if player_score.score >= 500 {
                msg!("Player score before deduction: {}", player_score.score);
                player_score.score -= 500;
                msg!("Player score after deduction: {}", player_score.score);
            } else {
                msg!("Player score is less than 500. No deduction will be made.");
            }
        }
        1 => {
            let new_score = u64::from_le_bytes(input[1..].try_into().unwrap());
            player_score.score = new_score;
        }
        _ => return Err(ProgramError::InvalidInstructionData),
    }

    PlayerScore::pack(player_score, &mut account.data.borrow_mut())?;

    Ok(())
}
