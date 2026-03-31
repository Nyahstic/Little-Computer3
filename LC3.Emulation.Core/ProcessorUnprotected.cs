using System.Reflection.Emit;

namespace LC3.Emulation.Core
{
    /// <summary>
    /// Creates a LittleComputer-3 processor with NO protection/interrupt handling.
    /// </summary>
    /// <remarks>
    /// The CPU doesn't have any direct connection to the outside world, but you can use the I/O stack and events.
    /// </remarks>
    public class ProcessorUnprotected
    {
        public const int CPU_OUTPUT_CHARACTER = 0;
        public const int CPU_OUTPUT_STRING = 1;
        public const int CPU_OUTPUT_HALT = 2;

        public const int CPU_INPUT_CHARACTER = 0;

        public ProcessorUnprotected(ushort ProgramCouterStart = 0x3000)
        {
            RegisterFile[(int)Register.R_PC] = ProgramCouterStart; // Default starting address for the program counter
            MemoryWrite(0x3000, 0b0001000000100001);
            MemoryWrite(0x3001, 0b0001001001100001);
            MemoryWrite(0x3002, 0b0001010010100001);
            MemoryWrite(0x3003, 0b0001011011100001);
            MemoryWrite(0x3004, 0b0001100100100001);
            MemoryWrite(0x3005, 0b0001101101100001);
            MemoryWrite(0x3006, 0b0001110110100001);
            MemoryWrite(0x3007, 0b1111000000100101);
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
            R_R5,
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

        public enum Flags
        {
            FL_POS = 1 << 0, /* P */
            FL_ZRO = 1 << 1, /* Z */
            FL_NEG = 1 << 2, /* N */
        };


        ushort _OpCodeFirstRegister, _OpCodeSecondRegister, _OpCodeThirdRegister, _Immediate5Bit, _PCOffset9Bit, _ConditionalFlag, _LoadRegisterOffset;
        // HACKHACK: condFlag SHOULD be a bool, but i want to use against a int, so it'll become an ushort
        bool _ImmediateFlag, _LongFlag;
        public void Step()
        {
            ushort instruction = MemoryRead(RegisterFile[(int)Register.R_PC]++);

            ushort opcode = (ushort)(instruction >> 12);
            

            _OpCodeFirstRegister = (ushort)((instruction >> 9) & 0x7);
            _OpCodeSecondRegister = (ushort)((instruction >> 6) & 0x7);
            _OpCodeThirdRegister = (ushort)(instruction & 0x7);
            _Immediate5Bit = _signExtend((ushort)(instruction & 0x1F), 5);
            _PCOffset9Bit = _signExtend((ushort)(instruction & 0x1FF), 9);
            _ImmediateFlag = ((instruction >> 5) & 0x1) == 1;
            _ConditionalFlag = (ushort)((instruction >> 9) & 0x7);
            _LongFlag = ((instruction >> 11) & 0x1) == 1;
            _LoadRegisterOffset = _signExtend((ushort)(instruction & 0x3F), 6);

            //OnDebugInfo?.Invoke($"[DEBUG] Executing instruction at address: 0x{RegisterFile[(int)Register.R_PC]:X4}, Instruction: 0x{memory[RegisterFile[(int)Register.R_PC]]:X4}");
            //OnDebugInfo?.Invoke($"[DEBUG] Decoded instruction - Opcode: {(OpCode)opcode}");

            switch (opcode)
            {
                case (ushort)OpCode.OP_BR:
                    if ((_ConditionalFlag & RegisterFile[(int)Register.R_COND]) != 0)
                    {
                        RegisterFile[(int)Register.R_PC] += _PCOffset9Bit;
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_BR, branching to address: 0x{RegisterFile[(int)Register.R_PC]:X4} with offset {_PCOffset9Bit}");
                    }
                    break;

                case (ushort)OpCode.OP_ADD:
                    if (_ImmediateFlag)
                    {
                        RegisterFile[_OpCodeFirstRegister] = (ushort)(RegisterFile[_OpCodeSecondRegister] + _Immediate5Bit);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_ADD with immediate value: R{_OpCodeFirstRegister} = R{_OpCodeSecondRegister} + {_Immediate5Bit} => 0x{RegisterFile[_OpCodeFirstRegister]:X4}");
                    }
                    else
                    {
                        RegisterFile[_OpCodeFirstRegister] = (ushort)(RegisterFile[_OpCodeSecondRegister] + RegisterFile[_OpCodeThirdRegister]);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_ADD with register value: R{_OpCodeFirstRegister} = R{_OpCodeSecondRegister} + R{_OpCodeThirdRegister} => 0x{RegisterFile[_OpCodeFirstRegister]:X4}");
                    }

                    _updateFlags(_OpCodeFirstRegister);
                    break;

                case (ushort)OpCode.OP_LD:
                    RegisterFile[_OpCodeFirstRegister] = MemoryRead((ushort)(RegisterFile[(int)Register.R_PC] + _PCOffset9Bit));
                    _updateFlags(_OpCodeFirstRegister);
                    break;

                case (ushort)OpCode.OP_ST:
                    MemoryWrite((ushort)(RegisterFile[(int)Register.R_PC] + _PCOffset9Bit), RegisterFile[_OpCodeFirstRegister]);
                    break;

                case (ushort)OpCode.OP_JSR:
                    RegisterFile[(int)Register.R_R7] = RegisterFile[(int)Register.R_PC];
                    if (_LongFlag)
                    {
                        //because longPCOffset is only used here, i will NOT put it at the start of the function
                        ushort longPCOffset = _signExtend((ushort)(instruction & 0x7FF), 11);
                        RegisterFile[(int)Register.R_PC] += longPCOffset;
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_JSR with long offset: Jumping to address: 0x{RegisterFile[(int)Register.R_PC]:X4} with long offset {longPCOffset}");
                    }
                    else // Appently JSRR here
                    {
                        RegisterFile[(int)Register.R_PC] = RegisterFile[_OpCodeSecondRegister];
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_JSR with register value: Jumping to address: 0x{RegisterFile[(int)Register.R_PC]:X4} from Register {(Register)_OpCodeSecondRegister}");
                    }
                    break;

                case (ushort)OpCode.OP_AND:
                    if (_ImmediateFlag)
                    {
                        RegisterFile[_OpCodeFirstRegister] = (ushort)(RegisterFile[_OpCodeSecondRegister] & _Immediate5Bit);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_AND with immediate value: {(Register)_OpCodeFirstRegister} = {(Register)_OpCodeSecondRegister} & {_Immediate5Bit} => 0x{RegisterFile[_OpCodeFirstRegister]:X4}");
                    }
                    else
                    {
                        RegisterFile[_OpCodeFirstRegister] = (ushort)(RegisterFile[_OpCodeSecondRegister] & RegisterFile[_OpCodeThirdRegister]);
                        OnDebugInfo?.Invoke($"[DEBUG] Executed OP_AND with register value: {(Register)_OpCodeFirstRegister} = {(Register)_OpCodeSecondRegister} & {(Register)_OpCodeThirdRegister} => 0x{RegisterFile[_OpCodeFirstRegister]:X4}");
                    }
                    _updateFlags(_OpCodeFirstRegister);
                    break;

                case (ushort)OpCode.OP_LDR:
                    RegisterFile[_OpCodeFirstRegister] = MemoryRead((ushort)(RegisterFile[_OpCodeSecondRegister] + _LoadRegisterOffset));
                    _updateFlags(_OpCodeFirstRegister);
                    break;

                case (ushort)OpCode.OP_STR:
                    MemoryWrite(MemoryRead((ushort)(RegisterFile[_OpCodeSecondRegister] + _LoadRegisterOffset)), RegisterFile[_OpCodeFirstRegister]);
                    break;

                case (ushort)OpCode.OP_RTI:
                    throw new InvalidLC3ProgramException("The program tried to execute a ReTurn from Interrupt from a non-protected CPU!", this);

                case (ushort)OpCode.OP_NOT:
                    RegisterFile[_OpCodeFirstRegister] = (ushort)~RegisterFile[_OpCodeSecondRegister];
                    OnDebugInfo?.Invoke($"[DEBUG] Executed OP_NOT, on Register {(Register)_OpCodeFirstRegister}, with Register {(Register)_OpCodeSecondRegister}");
                    _updateFlags(_OpCodeFirstRegister);
                    break;

                case (ushort)OpCode.OP_LDI:
                    RegisterFile[_OpCodeFirstRegister] = MemoryRead((ushort)(RegisterFile[(int)Register.R_PC] + _PCOffset9Bit));
                    OnDebugInfo?.Invoke($"[DEBUG] Executing OP_LDI, on Register {(Register)_OpCodeFirstRegister}, with offset of {_PCOffset9Bit}");
                    _updateFlags(_OpCodeFirstRegister);
                    break;

                case (ushort)OpCode.OP_STI:
                    MemoryWrite(MemoryRead((ushort)(RegisterFile[(int)Register.R_PC] + _PCOffset9Bit)), RegisterFile[_OpCodeFirstRegister]);
                    break;

                case (ushort)OpCode.OP_JMP: // Also RET????
                    RegisterFile[(int)Register.R_PC] = RegisterFile[_OpCodeSecondRegister];
                    OnDebugInfo?.Invoke($"[DEBUG] Executed OP_JMP, Jumping to address: 0x{RegisterFile[(int)Register.R_PC]:X4} from Register {(Register)_OpCodeSecondRegister}");
                    break;

                case (ushort)OpCode.OP_RES:
                    throw new NotImplementedException($"please fix: {nameof(opcode)} got an yet-to-be implemented: OpCode.{(OpCode)opcode}");
                    break;

                case (ushort)OpCode.OP_LEA:
                    RegisterFile[_OpCodeFirstRegister] = (ushort)(RegisterFile[(int)Register.R_PC] + _PCOffset9Bit);
                    _updateFlags(_OpCodeFirstRegister);
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
            if (!DoTrapInCSharp)
            {
                return;
            }
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
                    outputStack.Push((char)(RegisterFile[_OpCodeFirstRegister]));
                    break;
                case TrapCode.TRAP_PUTS:
                    ushort address = RegisterFile[_OpCodeFirstRegister];
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
                    address = RegisterFile[_OpCodeFirstRegister];
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
            return false;
            //if (!File.Exists(path))
            //{
            //    OnDebugInfo?.Invoke($"[FILE] Image path does not exist: {path}");
            //    return false;
            //}
            //else
            //{
            //    ushort origin = 0;
            //    OnDebugInfo?.Invoke($"Reading file {path}...");
            //    var fileFS = File.OpenRead(path);
            //    var fileBR = new BinaryReader(fileFS);
            //    origin = _swapEndianess(fileBR.ReadUInt16());
            //    ushort[] tempFile = new ushort[(fileFS.Length - 2) / 2];
            //    int i = 0;

            //    while (fileFS.Position <= (fileFS.Length - 2))
            //    {
            //        ushort word = fileBR.ReadUInt16(); //_swapEndianess(fileBR.ReadUInt16());
            //        MemoryWrite((ushort)(origin + i), word);
            //        tempFile[i] = word;
            //        i++;
            //    }
            //    File.WriteAllBytes($"{path}.read.bin", tempFile.SelectMany(BitConverter.GetBytes).ToArray());
            //    return true;
            //}
        }

        private ushort _swapEndianess(ushort value)
        {
            return (ushort)((value << 8) | (value >> 8));
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

        public void Run(bool alsoReset = false)
        {
            if (alsoReset)
            {
                RegisterFile[(int)Register.R_PC] = 0x3000; // Reset to default starting address
                for (int i = 0; i < RegisterFile.Length; i++)
                {
                    RegisterFile[i] = 0;
                }
            }
            RegisterFile[(int)Register.R_COND] = (ushort)Flags.FL_ZRO; 
            Running = true;
            while (Running)
            {
                Step();
            }
        }
    }
}   
