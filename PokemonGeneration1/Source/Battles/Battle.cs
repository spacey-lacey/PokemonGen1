﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonGeneration1.Source.Battles
{
    /// <summary>
    /// Representation of a Pokemon battle as a 
    /// Finite State Machine
    /// </summary>
    public class Battle
    {
        private Side PlayerSide;
        private Side OpponentSide;
        private BattleActor PlayerActor;
        private BattleActor OpponentActor;
        private BattleState State;

        //references to PlayerSide, OpponentSide, PlayerActor, & OpponentActor, but ordered
        private Side FirstSide;
        private Side SecondSide;
        private BattleActor FirstActor;
        private BattleActor SecondActor;



        public event EventHandler<BattleEventArgs> BattleBegun;
        public event EventHandler<BattleEventArgs> MakingSelections;
        public event EventHandler<BattleEventArgs> FirstExecutionBegun;
        public event EventHandler<BattleEventArgs> FirstExecutionOver;
        public event EventHandler<BattleEventArgs> SecondExecutionBegun;
        public event EventHandler<BattleEventArgs> SecondExecutionOver;
        public event EventHandler<BattleEventArgs> BattleOver;
        protected virtual void OnBattleBegun()
        {
            BattleEventArgs args = new BattleEventArgs();
            args.thisBattle = this;
            BattleBegun?.Invoke(this, args);
        }
        protected virtual void OnBattleOver()
        {
            BattleEventArgs args = new BattleEventArgs();
            args.thisBattle = this;
            BattleOver?.Invoke(this, args);
        }
        protected virtual void OnMakingSelections()
        {
            BattleEventArgs args = new BattleEventArgs();
            args.thisBattle = this;
            MakingSelections?.Invoke(this, args);
        }
        protected virtual void OnFirstExecutionBegun()
        {
            BattleEventArgs args = new BattleEventArgs();
            args.thisBattle = this;
            FirstExecutionBegun?.Invoke(this, args);
        }
        protected virtual void OnFirstExecutionOver()
        {
            BattleEventArgs args = new BattleEventArgs();
            args.thisBattle = this;
            FirstExecutionOver?.Invoke(this, args);
        }
        protected virtual void OnSecondExecutionBegun()
        {
            BattleEventArgs args = new BattleEventArgs();
            args.thisBattle = this;
            SecondExecutionBegun?.Invoke(this, args);
        }
        protected virtual void OnSecondExecutionOver()
        {
            BattleEventArgs args = new BattleEventArgs();
            args.thisBattle = this;
            SecondExecutionOver?.Invoke(this, args);
        }



        public bool IsGameOver() { return PlayerSide.IsDefeated() || OpponentSide.IsDefeated(); }
        public bool IsPlayerDefeated() { return PlayerSide.IsDefeated(); }
        public bool IsOpponentDefeated() { return OpponentSide.IsDefeated(); }
        public Side GetPlayerSide() { return PlayerSide; }
        public Side GetOpponentSide() { return OpponentSide; }



        public Battle(Side playerSide, Side opponentSide, BattleActor playerActor, BattleActor opponentActor)
        {
            PlayerSide = playerSide;
            OpponentSide = opponentSide;
            PlayerActor = playerActor;
            OpponentActor = opponentActor;

            State = BattleState.Intro;
        }



        public void Play()
        {
            OnBattleBegun();
            while (State != BattleState.GameOver)
            {
                ExecuteAndAdvanceState();
            }
            OnBattleOver();
        }
        private void ExecuteAndAdvanceState()
        {
            switch (State)
            {
                case BattleState.Intro:
                    State = BattleState.SetSelections;
                    break;

                case BattleState.SetSelections:
                    OnMakingSelections();
                    UpdateForEndOfTurn();
                    ExecutionSetSelectionState();
                    State = BattleState.SetFirstAndSecond;
                    break;

                case BattleState.SetFirstAndSecond:
                    ExecuteSetFirstAndSecondState();
                    State = BattleState.FirstExecutes;
                    break;

                case BattleState.FirstExecutes:
                    OnFirstExecutionBegun();
                    FirstSide.ExecuteSelection();

                    if (FirstSide.IsPokemonFainted() &&
                        SecondSide.IsPokemonFainted())
                        State = BattleState.BothFaint;
                    else if (FirstSide.IsPokemonFainted())
                        State = BattleState.FirstFaintsEarly;
                    else if (SecondSide.IsPokemonFainted())
                        State = BattleState.SecondFaints;
                    else State = BattleState.SecondExecutes;
                    OnFirstExecutionOver();
                    break;

                case BattleState.SecondExecutes:
                    OnSecondExecutionBegun();
                    SecondSide.ExecuteSelection();

                    if (FirstSide.IsPokemonFainted() && SecondSide.IsPokemonFainted())
                        State = BattleState.BothFaint;
                    else if (FirstSide.IsPokemonFainted())
                        State = BattleState.FirstFaintsLate;
                    else if (SecondSide.IsPokemonFainted())
                        State = BattleState.SecondFaints;
                    else State = BattleState.SetSelections;

                    OnSecondExecutionOver();
                    break;

                case BattleState.FirstFaintsEarly:
                    if (IsGameOver()) State = BattleState.GameOver;
                    else State = BattleState.FirstFaintsEarlySwitch;
                    break;

                case BattleState.FirstFaintsEarlySwitch:
                    ExecuteForcedSwitchOnFirst();
                    State = BattleState.SecondExecutes;
                    break;

                case BattleState.BothFaint:
                    if (IsGameOver()) State = BattleState.GameOver;
                    else State = BattleState.BothPokemonSwitch;
                    break;

                case BattleState.SecondFaints:
                    if (IsGameOver()) State = BattleState.GameOver;
                    else State = BattleState.SecondSwitchesPokemon;
                    break;

                case BattleState.FirstFaintsLate:
                    if (IsGameOver()) State = BattleState.GameOver;
                    else State = BattleState.FirstFaintsLateSwitch;
                    break;

                case BattleState.FirstFaintsLateSwitch:
                    ExecuteForcedSwitchOnFirst();
                    State = BattleState.SetSelections;
                    break;

                case BattleState.SecondSwitchesPokemon:
                    ExecuteForcedSwitchOnSecond();
                    State = BattleState.SetSelections;
                    break;

                case BattleState.BothPokemonSwitch:
                    ExecuteForcedSwitchOnFirst();
                    ExecuteForcedSwitchOnSecond();
                    State = BattleState.SetSelections;
                    break;

            }
        }
        private void ExecutionSetSelectionState()
        {
            Selection playerSelection;
            Selection opponentSelection;
            //check, for both players, if engaged in a two-turn move which will prevent choosing
            if (!PlayerSide.GetCurrentBattlePokemon().IsTwoTurnMoveActive())
            {
                playerSelection = PlayerActor.MakeBeginningOfTurnSelection(this, PlayerSide);
            }
            else
            {
                playerSelection = Selection.MakeFight(PlayerSide.GetCurrentBattlePokemon(),
                                                      OpponentSide.GetCurrentBattlePokemon(),
                                                      PlayerSide.GetCurrentBattlePokemon().GetTwoTurnMove());
            }
            if (!OpponentSide.GetCurrentBattlePokemon().IsTwoTurnMoveActive())
            {
                opponentSelection = OpponentActor.MakeBeginningOfTurnSelection(this, OpponentSide);
            }
            else
            {
                opponentSelection = Selection.MakeFight(OpponentSide.GetCurrentBattlePokemon(),
                                                        PlayerSide.GetCurrentBattlePokemon(),
                                                        OpponentSide.GetCurrentBattlePokemon().GetTwoTurnMove());
            }
            PlayerSide.SetSelection(playerSelection);
            OpponentSide.SetSelection(opponentSelection);
            State = BattleState.SetFirstAndSecond;
        }
        private void UpdateForEndOfTurn()
        {
            PlayerSide.GetCurrentBattlePokemon().UpdateForEndOfTurn();
            OpponentSide.GetCurrentBattlePokemon().UpdateForEndOfTurn();
        }
        private void ExecuteSetFirstAndSecondState()
        {
            //CASE 1: player's priority higher than opponent's
            if (PlayerSide.GetSelectionPriority() > OpponentSide.GetSelectionPriority())
            {
                SetPlayerFirst();
            }
            //CASE 2: opponent's priority higher than player's
            else if (PlayerSide.GetSelectionPriority() < OpponentSide.GetSelectionPriority())
            {
                SetOpponentFirst();
            }
            //CASE 3: priorities are equal, neither selected a Fight move
            else if (PlayerSide.GetSelectionPriority() > 1 &&
                     OpponentSide.GetSelectionPriority() > 1)
            {
                SetPlayerFirst();
            }
            //CASE 4: priorities are equal, both selected a Fight move
            else
            {
                //CASE 4.1: player's speed is greater than or equal to than opponents
                if (PlayerSide.GetCurrentBattlePokemon().GetSpeed() >= OpponentSide.GetCurrentBattlePokemon().GetSpeed())
                {
                    SetPlayerFirst();
                }
                //CASE 4.2: opponent's speed is greater than player's
                else
                {
                    SetOpponentFirst();
                }
            }
        }
        private void SetPlayerFirst()
        {
            FirstSide = PlayerSide;
            FirstActor = PlayerActor;
            SecondSide = OpponentSide;
            SecondActor = OpponentActor;
        }
        private void SetOpponentFirst()
        {
            FirstSide = OpponentSide;
            FirstActor = OpponentActor;
            SecondSide = PlayerSide;
            SecondActor = PlayerActor;
        }
        private void ExecuteForcedSwitchOnFirst()
        {
            Selection selection = FirstActor.MakeForcedSwitchSelection(this, FirstSide);
            FirstSide.SetSelection(selection);
            FirstSide.ExecuteSelection();
        }
        private void ExecuteForcedSwitchOnSecond()
        {
            Selection selection = SecondActor.MakeForcedSwitchSelection(this, SecondSide);
            SecondSide.SetSelection(selection);
            SecondSide.ExecuteSelection();
        }



    }



}