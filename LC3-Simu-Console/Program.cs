namespace LC3;
public enum Register{
    R_R0 = 0,
    R_R1    ,
    R_R2    ,
    R_R3    ,
    R_R4    ,
    R_R5    ,
    R_R6    ,
    R_R7    ,
    R_PC    ,
    R_COND  
}
enum Opcodes : ushort
{
    OP_BR = 0b0000, // Branch
    OP_ADD = 0b0001, // Add
    OP_LD = 0b0010, // Load
    OP_ST = 0b0011, // Store
    OP_JSR = 0b0100, // Jump Register
    OP_AND = 0b0101, // Bitwise AND
    OP_LDR = 0b0110, // Load Register
    OP_STR = 0b0111, // Store Register
    OP_RTI = 0b1000, // Unused
    OP_NOT = 0b1001, // Bitwise NOT
    OP_LDI = 0b1010, // Load Indirect
    OP_STI = 0b1011, // Store Indirect
    OP_JMP = 0b1100, // Jump
    OP_RES = 0b1101, // Reserved (unused)
    OP_LEA = 0b1110, // Load Effective Address
    OP_TRAP = 0b1111    // Exclusive Trap
}


enum Flags : ushort
{
    FL_POS = 1 << 0, // P
    FL_ZRO = 1 << 1, // Z
    FL_NEG = 1 << 2  // N
}

enum TrapCodes : byte
{
    TRAP_MINTY_DEBUGPRINT = 0x10, // Debug print
    TRAP_GETC = 0x20, // Get char from keyboard, but not echo to the terminal
    TRAP_OUT = 0x21, // Output a character
    TRAP_PUTS = 0x22, // Output string (putchar for all chars)
    TRAP_IN = 0x23, // Get char from keyboard, and echo it to the terminal
    TRAP_PUTSP = 0x24, // output a byte string
    TRAP_HALT = 0x25 // Halts the program
}

public class LC_3
{
    static readonly ushort PC_START = 0x3000;
    static readonly int MEM_MAX = ushort.MaxValue + 1;

    static ushort[] mem = new ushort[MEM_MAX];

    static ushort[] reg = new ushort[10];


    public static void Main(string[] args)
    {
        //Console.CursorVisible = false;
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine();
            Console.CursorVisible = true;
            Environment.Exit(-2);
        };
        
        if (args.Length < 1)
        {
            Console.WriteLine("usage: LC3 <image-file 1>... <image-file n>");
            Environment.Exit(2);
        }

        foreach (var img in args)
        {
            if (!File.Exists(img))
            {
                Console.WriteLine($"Error: File '{img}' not found");
                Environment.Exit(2);
            }
            if (!read_image(img))
            {
                Console.WriteLine($"Error: Failed to read image '{img}'");
                Environment.Exit(2);
            }
        }

        //Thread.Sleep(10000);

        reg[(int)Register.R_COND] = (ushort)Flags.FL_ZRO;
        reg[(int)Register.R_PC] = PC_START;

        bool running = true;
        while (running)
        {
            ushort instr = mem_read(reg[(int)Register.R_PC]++);
            Opcodes op = (Opcodes)(instr >> 12);
            ushort pc_offset = 0;

            //Thread.Sleep(125);
            doDebugPrint();

            switch (op)
            {
                case Opcodes.OP_ADD:
                    ushort r0 = getRegister(instr);
                    ushort r1 = getRegister(instr, 6);
                    bool imm_flag = (bool)(((instr >> 5) & 1) == 1);
                    if (imm_flag)
                    {
                        ushort imm5 = sign_extend((ushort)(instr & 0x1F), 5);
                        reg[r0] = (ushort)(reg[r1] + imm5);
                    }
                    else
                    {
                        ushort r2 = getRegister(instr, 0);
                        reg[r0] = (ushort)(reg[r1] + reg[r2]);
                    }
                    update_flags(r0);
                    break;
                case Opcodes.OP_AND:
                    r0 = getRegister(instr);
                    r1 = getRegister(instr, 6);
                    imm_flag = (bool)(((instr >> 5) & 1) == 1);

                    if (imm_flag)
                    {
                        ushort imm5 = sign_extend((ushort)(instr & 0x1F), 5);
                        reg[r0] = (ushort)(reg[r1] & imm5);
                    }
                    else
                    {
                        ushort r2 = getRegister(instr, 0);
                        reg[r0] = (ushort)(reg[r1] & reg[r2]);
                    }
                    update_flags(r0);
                    break;
                case Opcodes.OP_NOT:
                    r0 = getRegister(instr);
                    r1 = getRegister(instr, 6);

                    reg[r0] = (ushort)(~reg[r1]);
                    update_flags(r0);
                    break;
                case Opcodes.OP_BR:
                    pc_offset = sign_extend((ushort)(instr & 0x1FF), 9);
                    ushort cond_flag = (ushort)((instr >> 9) & 0x7);
                    if ((cond_flag & reg[(int)Register.R_COND]) >= 1)
                    {
                        reg[(int)Register.R_PC] += pc_offset;
                    }
                    break;
                case Opcodes.OP_JMP:
                    r1 = getRegister(instr, 6);
                    reg[(int)Register.R_PC] = reg[r1];
                    break;
                case Opcodes.OP_JSR:
                    bool long_flag = (bool)(((instr >> 5) & 1) == 1);
                    reg[(int)Register.R_R7] = reg[(int)Register.R_PC];
                    if (long_flag)
                    {
                        ushort long_pc_offset = sign_extend((ushort)(instr & 0x7FF), 11);
                        reg[(int)Register.R_PC] += long_pc_offset; // JSR
                    }
                    else
                    {
                        r1 = getRegister(instr, 6);
                        reg[(int)Register.R_PC] = reg[r1]; // JSRR
                    }
                    break;
                case Opcodes.OP_LD:
                    r0 = getRegister(instr);
                    pc_offset = sign_extend((ushort)(instr & 0x1FF), 9);
                    reg[r0] = mem_read((ushort)(reg[(int)Register.R_PC] + pc_offset));
                    update_flags(r0);
                    break;
                case Opcodes.OP_LDI:
                    r0 = getRegister(instr);
                    pc_offset = sign_extend((ushort)(instr & 0x1FF), 9);
                    reg[r0] = mem_read(mem_read((ushort)(reg[(int)Register.R_PC] + pc_offset)));
                    update_flags(r0);
                    break;
                case Opcodes.OP_LDR:
                    r0 = getRegister(instr);
                    r1 = getRegister(instr, 6);
                    ushort offset = sign_extend((ushort)(instr & 0x3F), 6);
                    reg[r0] = mem_read((ushort)(reg[r1] + offset));
                    update_flags(r0);
                    break;
                case Opcodes.OP_LEA:
                    r0 = getRegister(instr);
                    pc_offset = sign_extend((ushort)(instr & 0x1FF), 9);
                    reg[r0] = (ushort)(reg[(int)Register.R_PC] + pc_offset);
                    update_flags(r0);
                    break;
                case Opcodes.OP_ST:
                    r0 = getRegister(instr);
                    pc_offset = sign_extend((ushort)(instr & 0x1FF), 9);
                    mem_write((ushort)(reg[(int)Register.R_PC] + pc_offset), reg[r0]);
                    break;
                case Opcodes.OP_STI:
                    r0 = getRegister(instr);
                    pc_offset = sign_extend((ushort)(instr & 0x1FF), 9);
                    mem_write(mem_read((ushort)(reg[(int)Register.R_PC] + pc_offset)), reg[r0]);
                    break;
                case Opcodes.OP_STR:
                    r0 = getRegister(instr);
                    r1 = getRegister(instr, 6);
                    offset = sign_extend((ushort)(instr & 0x3F), 6);
                    mem_write((ushort)(reg[r1] + offset), reg[r0]);
                    break;
                case Opcodes.OP_TRAP:
                    byte trapvect8 = (byte)(instr & 0xFF);

                    execute_trapcode(trapvect8);
                    break;
                case Opcodes.OP_RES:
                case Opcodes.OP_RTI:
                default:
                    //throw new InvalidProgramException("Invalid opcode");
                    Console.Beep();
                    Console.WriteLine($"INVALID OPCODE {op} at PC {reg[(int)Register.R_PC].ToString("X")}");
                    break;
            }
        }

        
            
        Console.WriteLine();
        Console.CursorVisible = true;

        void execute_trapcode(byte trapvect8)
        {
            reg[(int)Register.R_R7] = reg[(int)Register.R_PC];
            //Console.WriteLine(trapvect8);
            switch (trapvect8)
            {
                case (byte)TrapCodes.TRAP_MINTY_DEBUGPRINT:
                    Console.WriteLine("Little Computer 3: Debug Print");
                    Console.WriteLine("Registers:");
                    for (int i = 0; i < reg.Length; i++)
                    {
                        Console.Write($"{(Register)i}: ");
                        Console.Write(reg[i].ToString("4X"));
                        Console.Write(" ");
                        if (i == reg.Length - 1)
                        {
                            Console.Write("\n");
                        }
                    }
                    Console.WriteLine("Flags");
                    if ((reg[(int)Register.R_COND] & (ushort)Flags.FL_ZRO) == 1) Console.WriteLine("Zero");
                    if ((reg[(int)Register.R_COND] & (ushort)Flags.FL_NEG) == 1) Console.WriteLine("Negative");
                    if ((reg[(int)Register.R_COND] & (ushort)Flags.FL_POS) == 1) Console.WriteLine("Positive");
                    break;
                case (Byte)TrapCodes.TRAP_PUTS:
                    ushort ptr = reg[(int)Register.R_R0];
                    while (mem[ptr] != 0x0000)
                    {
                        Console.Write((char)mem[ptr]);
                        ++ptr;
                    }
                    break;
                case (Byte)TrapCodes.TRAP_GETC:
                    
                    reg[(int)Register.R_R0] = (ushort)Console.ReadKey(true).KeyChar;
                    update_flags((int)Register.R_R0);
                    break;
                case (Byte)TrapCodes.TRAP_IN:
                    Console.Write("> ");
                    reg[(int)Register.R_R0] = (ushort)Console.ReadKey(false).KeyChar;
                    update_flags((int)Register.R_R0);
                    break;
                case (Byte)TrapCodes.TRAP_OUT:
                    Console.Write((char)reg[(int)Register.R_R0]);
                    break;
                case (Byte)TrapCodes.TRAP_PUTSP:
                    ptr = reg[(int)Register.R_R0];
                    while (mem[ptr] != 0x0000)
                    {
                        // Two bytes per character
                        var ch1 = (char)(mem[ptr] & 0xff);
                        var ch2 = (char)(mem[ptr] >> 8);
                        Console.Write($"{ch2}{ch1}");
                        ++ptr;
                    }
                    break;
                case (Byte)TrapCodes.TRAP_HALT:
                    Console.WriteLine("\n\t\aCPU HALTED");
                    running = false;
                    break;
            }
        }

        ushort sign_extend(ushort value, int bit_count)
        {
            if ((bool)(((value >> (bit_count - 1)) & 1) == 1))
            {
                value |= (ushort)(0xFFFF << bit_count);
            }
            return value;
        }

        void update_flags(ushort register)
        {
            if (reg[(int)register] == 0)
            {
                reg[(int)Register.R_COND] = (ushort)Flags.FL_ZRO;
            }
            else if ((bool)((reg[(int)register] >> 15) == 1))
            {
                reg[(int)Register.R_COND] = (ushort)Flags.FL_NEG;
            }
            else
            {
                reg[(int)Register.R_COND] = (ushort)Flags.FL_POS;
            }
        }

        ushort getRegister(ushort instr, int offset = 9)
        {
            return (ushort)((instr >> offset) & 0x7);
        }


        bool read_image(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open))
                {
                    var br = new BinaryReader(fs);
                    // 1. Lê a origem (primeiros 2 bytes)
                    ushort origin = swap16(br.ReadUInt16());

                    // 2. Lê o resto do arquivo
                    while (fs.Position < (fs.Length / 2))
                    {
                        mem[origin++] = swap16(br.ReadUInt16());
                    }
                }
                return true;
            }
            catch {
#if DEBUG
                throw;
#else
                return false;
#endif
            }
        }


        ushort swap16(ushort value)
        {
            ushort big_endian_value = 0;
            big_endian_value = (ushort)((value << 8) | (value >> 8));
            return big_endian_value;
        }

        ushort mem_read(ushort addr)
        {
            //Thread.Sleep(100);
            //Console.WriteLine($"Memory read: ${addr.ToString("X")} {mem[addr].ToString("X")}");
            if (addr == 0xFE00) // Keyboard status
            {
                if (Console.KeyAvailable)
                {
                    //Thread.Sleep(100000);
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    mem[0xFE00] = (1 << 15);
                    mem[0xFE02] = key.KeyChar;
                }
                else
                {
                    mem[0xFE00] = 0;
                }
            }

            return mem[addr];
        }

        void mem_write(ushort addr, ushort value)
        {
            mem[addr] = value;
        }
    }

    private static void doDebugPrint()
    {

        Console.SetCursorPosition(0, 0);
        Console.WriteLine("Registers");
        for(int i = 0; i < reg.Length / 2; i++)
        {
            Console.Write($"{(Register)i}: ");
            Console.Write(reg[i].ToString("X4"));
            Console.Write("\t");

            if (((Register)(i + 5)) == Register.R_COND)
            {
                Console.Write("Flags: ");
                switch (reg[(int)Register.R_COND])
                {
                    case (ushort)Flags.FL_ZRO:
                        Console.Write("Zero");
                        break;
                    case (ushort)Flags.FL_NEG:
                        Console.Write("Negative");
                        break;
                    case (ushort)Flags.FL_POS:
                        Console.Write("Positive");
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Console.Write($"{(Register)(i + reg.Length / 2)}: ");
                Console.Write(reg[i + reg.Length / 2].ToString("X4"));
                Console.Write("\n");
            }
            if (i == (reg.Length / 2) - 1)
            {
                Console.Write("\n");
            }
        }
    }
}
