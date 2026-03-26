using System.Reflection.Emit;

namespace LC3.Emulation.Core
{
    public class Processor
    {
        public const int CPU_OUTPUT_CHARACTER = 0;
        public const int CPU_OUTPUT_STRING = 1;
        public const int CPU_OUTPUT_HALT = 2;

        public const int CPU_INPUT_CHARACTER = 0;

        public Processor(ushort ProgramCouterStart = 0x3000)
        {
            RegisterFile[(int)Register.R_PC] = ProgramCouterStart; // Default starting address for the program counter
            
            for (int i = 0; i < memory.Length; i++)
            {
                MemoryWrite((ushort)i, (ushort) Random.Shared.Next(0, ushort.MaxValue));
            }
        }


        // Event to signal debug information
        public event Action<string>? OnDebugInfo;
        public event Action<int>? OnCPUOutput;
        public event Action<int>? OnCPUInput;

        public Stack<char> inputStack = new Stack<char>();
        public Stack<char> outputStack = new Stack<char>();

        public bool Running = false;
        public bool DoTrapInCSharp = true;

        public enum Register{ 
            R_R0 = 0,
            R_R1,
            R_R2,
            R_R3,
            R_R4,
            R_R6,
            R_R7,
            R_PC,
            R_COND,
            R_COUNT
        };

        public enum TrapCode 
        {
            TRAP_GETC = 0x20,  /* get character from keyboard, not echoed onto the terminal */
            TRAP_OUT = 0x21,   /* output a character */
            TRAP_PUTS = 0x22,  /* output a word string */
            TRAP_IN = 0x23,    /* get character from keyboard, echoed onto the terminal */
            TRAP_PUTSP = 0x24, /* output a byte string */
            TRAP_HALT = 0x25   /* halt the program */
        };

        private ushort[] memory = new ushort[ushort.MaxValue + 1];
        public ushort[] RegisterFile { get; private set; } = new ushort[(int)Register.R_COUNT];
        
        public enum OpCode 
        {
            OP_BR = 0, /* branch */
            OP_ADD,    /* add  */
            OP_LD,     /* load */
            OP_ST,     /* store */
            OP_JSR,    /* jump register */
            OP_AND,    /* bitwise and */
            OP_LDR,    /* load register */
            OP_STR,    /* store register */
            OP_RTI,    /* unused */
            OP_NOT,    /* bitwise not */
            OP_LDI,    /* load indirect */
            OP_STI,    /* store indirect */
            OP_JMP,    /* jump */
            OP_RES,    /* reserved (unused) */
            OP_LEA,    /* load effective address */
            OP_TRAP    /* execute trap */
        };

        [Flags]
        public enum Flags
        {
            FL_POS = 1 << 0, /* P */
            FL_ZRO = 1 << 1, /* Z */
            FL_NEG = 1 << 2, /* N */
        };


        ushort r0, r1, r2, imm5, pcOffset9, condFlag, loadRegisterOffset;
        // HACKHACK: condFlag SHOULD be a bool, but i want to use against a int, so it'll become an ushort
        bool immFlag, longFlag;
        public void Step()
        {
            RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_ZRO;


            ushort instruction = MemoryRead(RegisterFile[(int)Register.R_PC]++);

            ushort opcode = (ushort)(instruction >> 12);
            

            r0 = (ushort)((instruction >> 9) & 0x7);
            r1 = (ushort)((instruction >> 6) & 0x7);
            r2 = (ushort)(instruction & 0x7);
            imm5 = _signExtend((ushort)(instruction & 0x1F), 5);
            pcOffset9 = _signExtend((ushort)(instruction & 0x1FF), 9);
            immFlag = ((instruction >> 5) & 0x1) == 1;
            condFlag = (ushort)((instruction >> 9) & 0x7);
            longFlag = ((instruction >> 11) & 0x1) == 1;
            loadRegisterOffset = _signExtend((ushort)(instruction & 0x3F), 6);

            //OnDebugInfo?.Invoke($"[DEBUG] Executing instruction at address: 0x{RegisterFile[(int)Register.R_PC]:X4}, Instruction: 0x{memory[RegisterFile[(int)Register.R_PC]]:X4}");
            //OnDebugInfo?.Invoke($"[DEBUG] Decoded instruction - Opcode: {(OpCode)opcode}");

            switch (opcode)
            {
                case (ushort)OpCode.OP_BR:
                    if ((condFlag & RegisterFile[(int)Register.R_COND]) == 1)
                    {
                        RegisterFile[(int)Register.R_PC] += pcOffset9;
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_BR, branching to address: 0x{RegisterFile[(int)Register.R_PC]:X4} with offset {pcOffset9}");
                    }
                    break;

                case (ushort)OpCode.OP_ADD:
                    if (immFlag)
                    {
                        RegisterFile[r0] = (ushort)(RegisterFile[r1] + imm5);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_ADD with immediate value: R{r0} = R{r1} + {imm5} => 0x{RegisterFile[r0]:X4}");
                    }
                    else
                    {
                        RegisterFile[r0] = (ushort)(RegisterFile[r1] + RegisterFile[r2]);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_ADD with register value: R{r0} = R{r1} + R{r2} => 0x{RegisterFile[r0]:X4}");
                    }

                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_LD:
                    RegisterFile[r0] = MemoryRead((ushort)(RegisterFile[(int)Register.R_PC] + pcOffset9));
                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_ST:
                    MemoryWrite((ushort)(RegisterFile[(int)Register.R_PC] + pcOffset9), RegisterFile[r0]);
                    break;

                case (ushort)OpCode.OP_JSR:
                    RegisterFile[(int)Register.R_R7] = RegisterFile[(int)Register.R_PC];
                    if (longFlag)
                    {
                        //because longPCOffset is only used here, i will NOT put it at the start of the function
                        ushort longPCOffset = _signExtend((ushort)(instruction & 0x7FF), 11);
                        RegisterFile[(int)Register.R_PC] += longPCOffset;
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_JSR with long offset: Jumping to address: 0x{RegisterFile[(int)Register.R_PC]:X4} with long offset {longPCOffset}");
                    }
                    else // Appently JSRR here
                    {
                        RegisterFile[(int)Register.R_PC] = RegisterFile[r1];
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_JSR with register value: Jumping to address: 0x{RegisterFile[(int)Register.R_PC]:X4} from Register {(Register)r1}");
                    }
                    break;

                case (ushort)OpCode.OP_AND:
                    if (immFlag)
                    {
                        RegisterFile[r0] = (ushort)(RegisterFile[r1] & imm5);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_AND with immediate value: {(Register)r0} = {(Register)r1} & {imm5} => 0x{RegisterFile[r0]:X4}");
                    }
                    else
                    {
                        RegisterFile[r0] = (ushort)(RegisterFile[r1] & RegisterFile[r2]);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_AND with register value: {(Register)r0} = {(Register)r1} & {(Register)r2} => 0x{RegisterFile[r0]:X4}");
                    }
                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_LDR:
                    RegisterFile[r0] = MemoryRead((ushort)(RegisterFile[r1] + loadRegisterOffset));
                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_STR:
                    MemoryWrite(MemoryRead((ushort)(RegisterFile[r1] + loadRegisterOffset)), RegisterFile[r0]);
                    break;

                case (ushort)OpCode.OP_RTI:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_NOT:
                    RegisterFile[r0] = (ushort)~RegisterFile[r1];
                    OnDebugInfo?.Invoke($"[DEBUG] Executed OP_NOT, on Register {(Register)r0}, with Register {(Register)r1}");
                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_LDI:
                    RegisterFile[r0] = MemoryRead((ushort)(RegisterFile[(int)Register.R_PC] + pcOffset9));
                    OnDebugInfo?.Invoke($"[DEBUG] Executing OP_LDI, on Register {(Register)r0}, with offset of {pcOffset9}");
                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_STI:
                    MemoryWrite(MemoryRead((ushort)(RegisterFile[(int)Register.R_PC] + pcOffset9)), RegisterFile[r0]);
                    break;

                case (ushort)OpCode.OP_JMP: // Also RET????
                    RegisterFile[(int)Register.R_PC] = RegisterFile[r1];
                    OnDebugInfo?.Invoke($"[DEBUG] Executed OP_JMP, Jumping to address: 0x{RegisterFile[(int)Register.R_PC]:X4} from Register {(Register)r1}");
                    break;

                case (ushort)OpCode.OP_RES:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_LEA:
                    RegisterFile[r0] = (ushort)(RegisterFile[(int)Register.R_PC] + pcOffset9);
                    _updateFlags(r0);
                    break;

                case (ushort)OpCode.OP_TRAP:
                    RegisterFile[(int)Register.R_R7] = RegisterFile[(int)Register.R_PC];
                    _handleTrapCode((TrapCode)(instruction & 0xFF));
                    break;

                 default:
                    throw new InvalidOperationException($"Unknown opcode: {opcode}");
            }
        }

        private void _handleTrapCode(TrapCode trapCode)
        {
            switch (trapCode)
            {
                case TrapCode.TRAP_GETC:
                    bool alertConsumer = false;
                    while(inputStack.Count == 0)
                    {
                        if (!alertConsumer)
                        {
                            OnDebugInfo?.Invoke($"[DEBUG] Waiting for input for TRAP_GETC...");
                            OnCPUInput?.Invoke(CPU_INPUT_CHARACTER);
                            alertConsumer = true;
                        }
                        // Wait for input
                    }
                    RegisterFile[(int)Register.R_R0] = (ushort)inputStack.Pop();
                    _updateFlags(0);
                    break;
                case TrapCode.TRAP_OUT:
                    OnCPUOutput?.Invoke(CPU_OUTPUT_CHARACTER);
                    outputStack.Push((char)(RegisterFile[r0]));
                    break;
                case TrapCode.TRAP_PUTS:
                    ushort address = RegisterFile[r0];
                    while (MemoryRead(address) != 0)
                    {
                        char c = (char)(MemoryRead(address) & 0xFF);
                        outputStack.Push(c);
                        OnCPUOutput?.Invoke(CPU_OUTPUT_CHARACTER);
                        address++;
                    }
                    break;
                case TrapCode.TRAP_IN:
                    alertConsumer = false;
                    while (inputStack.Count == 0)
                    {
                        if (!alertConsumer)
                        {
                            OnDebugInfo?.Invoke($"[DEBUG] Waiting for input for TRAP_IN...");
                            OnCPUInput?.Invoke(CPU_INPUT_CHARACTER);
                            alertConsumer = true;
                        }
                        // Wait for input
                    }
                    RegisterFile[(int)Register.R_R0] = (ushort)inputStack.Pop();
                    _updateFlags(0);
                    break;
                case TrapCode.TRAP_PUTSP:
                    address = RegisterFile[r0];
                    while(MemoryRead(address) != 0)
                    {
                        char c1, c2;
                        var val = MemoryRead(++address);
                        c1 = (char)(MemoryRead(address) & 0xFF);
                        c2 = (char)(MemoryRead(address) >> 8);
                        outputStack.Push(c1);
                        outputStack.Push(c2);
                    }
                    OnCPUOutput.Invoke(CPU_OUTPUT_STRING);
                    break;
                default:
                case TrapCode.TRAP_HALT:
                    Running = false;
                    OnCPUOutput?.Invoke(CPU_OUTPUT_HALT);
                    break;
            }
        }

        public ushort MemoryRead(ushort Address)
        {
            return memory[Address % ushort.MaxValue];
        }

        public void MemoryWrite(ushort Address, ushort Value)
        {
            memory[Address % ushort.MaxValue] = Value;
        }

        public bool LoadImage(string path) {
            if (!File.Exists(path))
            {
                OnDebugInfo?.Invoke($"[FILE] Image path does not exist: {path}");
                return false;
            }

            return false;
        }

        private ushort _signExtend(ushort value, int bitCount)
        {
            if (((value >> (bitCount - 1)) & 1) == 1)
            {
                value |= (ushort)(0xFFFF << bitCount);
            }
            return value;
        }

        private void _updateFlags(ushort RegisterToCheck)
        {
            if (RegisterFile[RegisterToCheck] == 0) {
                RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_ZRO;
            } 
            else if ((RegisterFile[RegisterToCheck] >> 15) != 0)
            {
                RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_NEG;
            }
            else
            {
                RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_POS;
            }
        }
    }
}   
