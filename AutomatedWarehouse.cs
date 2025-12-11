//-----------------------------------------------------------------------------
// FACTORY I/O (SDK)
//
// Copyright (C) Real Games. All rights reserved.
//-----------------------------------------------------------------------------

using EngineIO;
using System;

namespace Controllers
{
    public class AutomatedWarehouse : Controller
    {
        MemoryBit entryConveyor = MemoryMap.Instance.GetBit("Entry Conveyor", MemoryType.Output);
        MemoryBit loadConveyor = MemoryMap.Instance.GetBit("Load Conveyor", MemoryType.Output);
        MemoryInt targetPosition = MemoryMap.Instance.GetInt("Target Position", MemoryType.Output);
        MemoryBit forksLeft = MemoryMap.Instance.GetBit("Forks Left", MemoryType.Output);
        MemoryBit lift = MemoryMap.Instance.GetBit("Lift", MemoryType.Output);
        MemoryBit forksRight = MemoryMap.Instance.GetBit("Forks Right", MemoryType.Output);

        MemoryBit movingX = MemoryMap.Instance.GetBit("Moving X", MemoryType.Input);
        MemoryBit movingZ = MemoryMap.Instance.GetBit("Moving Z", MemoryType.Input);
        MemoryBit atLoad = MemoryMap.Instance.GetBit("At Load", MemoryType.Input);
        MemoryBit atLeft = MemoryMap.Instance.GetBit("At Left", MemoryType.Input);
        MemoryBit atMiddle = MemoryMap.Instance.GetBit("At Middle", MemoryType.Input);
        MemoryBit atRight = MemoryMap.Instance.GetBit("At Right", MemoryType.Input);

        FTRIG ftMovXZ = new FTRIG();
        RTRIG rtAtLoad = new RTRIG();
        RTRIG rtAtLeft = new RTRIG();
        FTRIG ftMovZ = new FTRIG();
        RTRIG rtAtMiddle = new RTRIG();
        RTRIG rtAtRight = new RTRIG();

        int currentPos = 0;

        State state = State.State0;
        //my Variables
        int[] warehouseStorage = new int[54]; //move to program file
        int storeLocation = 0;
        bool manual = true; //The factory's default. Places boxes sequentially.
        int choice = 0;
        int pickupLocation = 0;

        public AutomatedWarehouse()
        {
            state = State.State1;

            entryConveyor.Value = false;
            loadConveyor.Value = false;

            entryConveyor.Value = false;
            loadConveyor.Value = false;
            targetPosition.Value = 55;
            forksLeft.Value = false;
            lift.Value = false;
            forksRight.Value = false;
        }

        public override void Execute(int elapsedMilliseconds)
        {
            ftMovXZ.CLK(movingX.Value || movingZ.Value);
            rtAtLoad.CLK(!atLoad.Value);
            rtAtLeft.CLK(atLeft.Value);
            ftMovZ.CLK(movingZ.Value);
            rtAtMiddle.CLK(atMiddle.Value);
            rtAtRight.CLK(atRight.Value);

            
            if (state == State.State11)
            {
                Console.WriteLine("What box do you want to pick up?");
                Console.WriteLine("0: from the conveyor; 1-54: from storage");
                pickupLocation = Convert.ToInt16(Console.ReadLine());
                if (pickupLocation < 1)
                {
                    targetPosition.Value = 55;
                    state = State.State0;
                }
                else if(pickupLocation == targetPosition.Value)
                {
                    warehouseStorage[pickupLocation] = 0;
                    state = State.State13;//if picking up from the same place skip a state
                }
                else
                {
                    targetPosition.Value = pickupLocation;
                    warehouseStorage[pickupLocation] = 0;
                    state = State.State12; 
                }
                
            }

            else if(state == State.State12)
            {
                //moving to pickup cell
                if (ftMovXZ.Q)
                {
                    state = State.State13;
                }
            }

            else if(state == State.State13)
            {
                forksRight.Value = true;
                if (rtAtRight.Q)
                {
                    state = State.State14;
                }
            }
            else if(state == State.State14)
            {
                lift.Value = true;
                if (ftMovZ.Q)
                    state = State.State15;
            }
            else if (state == State.State15)
            {
                forksRight.Value = false;
                if (rtAtMiddle.Q)
                    state = State.State16;
            }

            else if (state == State.State16)
            {
                Console.WriteLine("Where do you want to store this box? (1-54)");
                storeLocation = 0;
                while (storeLocation == 0)
                {
                    storeLocation = Convert.ToInt16(Console.ReadLine());
                    Console.WriteLine(storeLocation);

                    if (warehouseStorage[storeLocation] == 1)
                    {
                        Console.WriteLine("Storage is already allocated pick another place");
                        storeLocation = 0;
                    }
                    
                }
                warehouseStorage[storeLocation] = 1;
                state = State.State5;
            }


            else if (state == State.State0)
            {
                storeLocation = 0;
                //targetPosition.Value = 55; //rest position

               
                if (ftMovXZ.Q)
                    state = State.State1;
            }
            else if (state == State.State1)
            {
                
                //Waiting for load...
                //if manual
                if (manual)
                {
                   while (storeLocation == 0)
                    {
                        

                        Console.WriteLine("Where do you want to store this box? (1-54)");

                        storeLocation = Convert.ToInt16(Console.ReadLine());
                        Console.WriteLine(storeLocation);

                        if (warehouseStorage[storeLocation] == 1)
                        {
                            Console.WriteLine("Storage is already allocated pick another place");
                            storeLocation = 0;
                        }

                    }
                    warehouseStorage[storeLocation] = 1;
                }
                else
                {
                    for(int i = 0; i < warehouseStorage.Length; i++)
                    {
                        if (warehouseStorage[i] == 0)//found empty spot
                        {
                            storeLocation = i;
                            warehouseStorage[i] = 1; //sets storage to occupied
                        }
                    }
                }
 
                if (!atLoad.Value)
                    state = State.State2;
            }
            else if (state == State.State2)
            {
                forksLeft.Value = true;

                if (rtAtLeft.Q)
                    state = State.State3;
            }
            else if (state == State.State3)
            {
                lift.Value = true;

                if (ftMovZ.Q)
                    state = State.State4;
            }
            else if (state == State.State4)
            {
                forksLeft.Value = false;

                if (rtAtMiddle.Q)
                    state = State.State5;
            }
            else if (state == State.State5)
            {
                targetPosition.Value = storeLocation;

                state = State.State6;
            }
            else if (state == State.State6)
            {
                //Moving to destination...

                if (ftMovXZ.Q)
                    state = State.State7;
            }
            else if (state == State.State7)
            {
                forksRight.Value = true;

                if (rtAtRight.Q)
                    state = State.State8;
            }
            else if (state == State.State8)
            {
                lift.Value = false;

                if (ftMovZ.Q)
                    state = State.State9;
            }
            else if (state == State.State9)
            {
                forksRight.Value = false;

                if (rtAtMiddle.Q)
                    
                    state = State.State11;
            }

            entryConveyor.Value = loadConveyor.Value = atLoad.Value;
        }
    }
}
