﻿using Ambermoon.Data;
using System;
using System.Linq;

namespace Ambermoon
{
    internal static class EventExtensions
    {
        public static Event ExecuteEvent(this Event @event, Map map, Game game,
            ref EventTrigger trigger, uint x, uint y, uint ticks, ref bool lastEventStatus,
            out bool aborted, IConversationPartner conversationPartner = null)
        {
            // Note: Aborted means that an event is not even executed. It does not mean that a decision
            // box is answered with No for example. It is used when:
            // - A condition of a condition event is not met and there is no event that is triggered in that case.
            // - A text popup does not accept the given trigger.
            // This is important in 3D when there might be an event on the current block and on the next one.
            // For example buttons use 2 events (one for Eye interaction and one for Hand interaction).

            aborted = false;

            switch (@event.Type)
            {
                // TODO ...
                case EventType.Teleport:
                {
                    if (trigger != EventTrigger.Move &&
                        trigger != EventTrigger.Always)
                        return null;

                    if (!(@event is TeleportEvent teleportEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid teleport event.");

                    game.Teleport(teleportEvent);
                    break;
                }
                case EventType.Door:
                {
                    if (!(@event is DoorEvent doorEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid door event.");

                    if (!game.ShowDoor(doorEvent, false, false, map))
                        aborted = true;
                    return null;
                }
                case EventType.Chest:
                {
                    if (trigger == EventTrigger.Mouth)
                    {
                        aborted = true;
                        return null;
                    }

                    if (!(@event is ChestEvent chestEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    if (chestEvent.Flags.HasFlag(ChestEvent.ChestFlags.SearchSkillCheck) &&
                        game.RandomInt(0, 99) >= game.CurrentPartyMember.Abilities[Ability.Searching].TotalCurrentValue)
                    {
                        aborted = true;
                        return null;
                    }

                    game.ShowChest(chestEvent, false, false, map);
                    return null;
                }
                case EventType.PopupText:
                {
                    if (!(@event is PopupTextEvent popupTextEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid text popup event.");

                    switch (trigger)
                    {
                        case EventTrigger.Move:
                            if (!popupTextEvent.CanTriggerByMoving)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        case EventTrigger.Eye:
                            if (!popupTextEvent.CanTriggerByCursor)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        case EventTrigger.Always:
                            break;
                        default:
                            aborted = true;
                            return null;
                    }

                    bool eventStatus = lastEventStatus;

                    game.ShowTextPopup(map, popupTextEvent, _ =>
                    {
                        map.TriggerEventChain(game, EventTrigger.Always,
                            x, y, game.CurrentTicks, @event.Next, eventStatus);
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.Spinner:
                {
                    if (trigger == EventTrigger.Eye)
                    {
                        game.ShowMessagePopup(game.DataNameProvider.SeeRoundDiskInFloor);
                        aborted = true;
                        return null;
                    }

                    if (trigger != EventTrigger.Move &&
                        trigger != EventTrigger.Always)
                        return null;

                    if (!(@event is SpinnerEvent spinnerEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid spinner event.");

                    game.Spin(spinnerEvent.Direction, spinnerEvent.Next);
                    break;
                }
                case EventType.Trap:
                {
                    if (!(@event is TrapEvent trapEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid trap event.");

                    if (trigger == EventTrigger.Eye)
                    {
                        // Note: Eye will only detect the trap, not trigger it!
                        game.ShowMessagePopup(game.DataNameProvider.YouNoticeATrap);
                        aborted = true;
                        return null;
                    }
                    else if (trigger != EventTrigger.Move && trigger != EventTrigger.Always)
                    {
                        aborted = true;
                        return null;
                    }

                    game.TriggerTrap(trapEvent);
                    return null; // next event is only executed after trap effect
                }
                // TODO ...
                case EventType.Riddlemouth:
                {
                    if (trigger != EventTrigger.Always &&
                        trigger != EventTrigger.Eye &&
                        trigger != EventTrigger.Hand &&
                        trigger != EventTrigger.Mouth)
                        return null;

                    if (!(@event is RiddlemouthEvent riddleMouthEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid riddle mouth event.");

                    game.ShowRiddlemouth(map, riddleMouthEvent, () =>
                    {
                        map.TriggerEventChain(game, EventTrigger.Always,
                            x, y, game.CurrentTicks, @event.Next, true);
                    });
                    return null; // next event is only executed after popup response
                }
                case EventType.ChangeTile:
                {
                    // TODO: add those to the savegame as well!
                    if (!(@event is ChangeTileEvent changeTileEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid chest event.");

                    game.UpdateMapTile(changeTileEvent, x, y);
                    break;
                }
                case EventType.StartBattle:
                {
                    if (!(@event is StartBattleEvent battleEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid battle event.");

                    game.StartBattle(battleEvent, battleEvent.Next, game.GetCombatBackgroundIndex(map, x, y));
                    return null;
                }
                case EventType.EnterPlace:
                {
                    if (!(@event is EnterPlaceEvent enterPlaceEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid place event.");

                    if (!game.EnterPlace(map, enterPlaceEvent))
                        aborted = true;
                    return null;
                }
                case EventType.Condition:
                {
                    if (!(@event is ConditionEvent conditionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid condition event.");

                    var mapEventIfFalse = conditionEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : map.Events[(int)conditionEvent.ContinueIfFalseWithMapEventIndex]; // TODO: is this right or +/- 1?

                    switch (conditionEvent.TypeOfCondition)
                    {
                        case ConditionEvent.ConditionType.GlobalVariable:
                            if (game.CurrentSavegame.GetGlobalVariable(conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.EventBit:
                            if (game.CurrentSavegame.GetEventBit(conditionEvent.ObjectIndex >> 6, conditionEvent.ObjectIndex & 0x3f) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.CharacterBit:
                            if (game.CurrentSavegame.GetCharacterBit(conditionEvent.ObjectIndex >> 5, conditionEvent.ObjectIndex & 0x1f) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Hand:
                            if (trigger != EventTrigger.Hand)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Eye:
                            if (trigger != EventTrigger.Eye)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.Success:
                            if (!lastEventStatus)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        case ConditionEvent.ConditionType.UseItem:
                        {
                            if (trigger < EventTrigger.Item0)
                            {
                                // no item used
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }

                            uint itemIndex = (uint)(trigger - EventTrigger.Item0);

                            if (itemIndex != conditionEvent.ObjectIndex)
                            {
                                // wrong item used
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.PartyMember:
                        {
                            if (game.PartyMembers.Any(m => m.Index == conditionEvent.ObjectIndex) != (conditionEvent.Value != 0))
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        }
                        case ConditionEvent.ConditionType.ItemOwned:
                        {
                            int totalCount = 0;

                            foreach (var partyMember in game.PartyMembers)
                            {
                                foreach (var slot in partyMember.Inventory.Slots)
                                {
                                    if (slot.ItemIndex == conditionEvent.ObjectIndex)
                                        totalCount += slot.Amount;
                                }
                                foreach (var slot in partyMember.Equipment.Slots)
                                {
                                    if (slot.Value.ItemIndex == conditionEvent.ObjectIndex)
                                        totalCount += slot.Value.Amount;
                                }
                            }

                            if (conditionEvent.Value == 0 && totalCount != 0)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            else if (conditionEvent.Value == 1 && totalCount == 0 || totalCount < conditionEvent.Count)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }

                            break;
                        }
                        case ConditionEvent.ConditionType.Levitating:
                            if (trigger != EventTrigger.Levitating)
                            {
                                aborted = mapEventIfFalse == null;
                                lastEventStatus = false;
                                return mapEventIfFalse;
                            }
                            break;
                        // TODO ...
                    }

                    trigger = EventTrigger.Always; // following events should not dependent on the trigger anymore

                    break;
                }
                case EventType.Action:
                {
                    if (!(@event is ActionEvent actionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid action event.");

                    switch (actionEvent.TypeOfAction)
                    {
                        case ActionEvent.ActionType.SetGlobalVariable:
                            game.CurrentSavegame.SetGlobalVariable(actionEvent.ObjectIndex, actionEvent.Value != 0);
                            break;
                        case ActionEvent.ActionType.SetEventBit:
                            game.SetMapEventBit(actionEvent.ObjectIndex >> 6, actionEvent.ObjectIndex & 0x3f, actionEvent.Value != 0);
                            break;
                        case ActionEvent.ActionType.SetCharacterBit:
                            game.SetMapCharacterBit(actionEvent.ObjectIndex >> 5, actionEvent.ObjectIndex & 0x1f, actionEvent.Value != 0);
                            break;
                            // TODO ...
                    }

                    break;
                }
                case EventType.Dice100Roll:
                {
                    if (!(@event is Dice100RollEvent diceEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid dice 100 event.");

                    var mapEventIfFalse = diceEvent.ContinueIfFalseWithMapEventIndex == 0xffff
                        ? null : map.Events[(int)diceEvent.ContinueIfFalseWithMapEventIndex]; // TODO: is this right or +/- 1?
                    lastEventStatus = game.RollDice100() < diceEvent.Chance;
                    return lastEventStatus ? diceEvent.Next : mapEventIfFalse;
                }
                case EventType.Conversation:
                {
                    if (!(@event is ConversationEvent conversationEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid conversation event.");

                    switch (conversationEvent.Interaction)
                    {
                        case ConversationEvent.InteractionType.Keyword:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        case ConversationEvent.InteractionType.ShowItem:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        case ConversationEvent.InteractionType.GiveItem:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        case ConversationEvent.InteractionType.Talk:
                            if (trigger != EventTrigger.Mouth)
                            {
                                aborted = true;
                                return null;
                            }
                            break;
                        case ConversationEvent.InteractionType.Leave:
                            // TODO: this has to be handled by the conversation window
                            aborted = true;
                            return null;
                        default:
                            // TODO
                            Console.WriteLine($"Found unknown conversation interaction type: {conversationEvent.Interaction}");
                            aborted = true;
                            return null;
                    }
                    game.ShowConversation(conversationPartner, conversationEvent.Next);
                    return null;
                }
                case EventType.PrintText:
                {
                    if (!(@event is PrintTextEvent printTextEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid print text event.");

                    // Note: This is only used by conversations and is handled by game.ShowConversation.
                    // So we don't need to do anything here.
                    return printTextEvent.Next;
                }
                // TODO ...
                case EventType.Decision:
                {
                    if (!(@event is DecisionEvent decisionEvent))
                        throw new AmbermoonException(ExceptionScope.Data, "Invalid decision event.");

                    game.ShowDecisionPopup(map, decisionEvent, response =>
                    {
                        if (response == PopupTextEvent.Response.Yes)
                        {
                            map.TriggerEventChain(game, EventTrigger.Always,
                                x, y, game.CurrentTicks, @event.Next, true);
                        }
                        else // Close and No have the same meaning here
                        {
                            if (decisionEvent.NoEventIndex != 0xffff)
                            {
                                map.TriggerEventChain(game, EventTrigger.Always,
                                    x, y, game.CurrentTicks, map.Events[(int)decisionEvent.NoEventIndex], false);
                            }
                        }
                    });
                    return null; // next event is only executed after popup response
                }
                // TODO ...
            }

            // TODO: battles, chest looting, etc should set this dependent on:
            // - battle won
            // - chest fully looted
            // ...
            // TODO: to do so we have to memorize the current event chain and continue
            // it after the player has done some actions (loot a chest, fight a battle, etc).
            // maybe we need a state machine here instead of just a linked list and a loop.
            lastEventStatus = true;

            return @event.Next;
        }

        public static bool TriggerEventChain(this Map map, Game game, EventTrigger trigger, uint x, uint y,
            uint ticks, Event firstMapEvent, bool lastEventStatus = false)
        {
            var mapEvent = firstMapEvent;

            while (mapEvent != null)
            {
                mapEvent = mapEvent.ExecuteEvent(map, game, ref trigger, x, y, ticks, ref lastEventStatus, out bool aborted);

                if (aborted)
                    return false;
            }

            return true;
        }
    }
}
