using System;
using System.Reflection;

namespace Secventiator
{
    class Program
    {
        //aici sunt ENUM-uri folosite pentru a face mai usoara referirea la toate cazurile in switch

        enum MEMORIE
        {
            NONE,
            IFCH,
            RD,
            WR
        };

        enum enumSBUS
        {
            NONE,
            PdFLAG,
            PdRG,
            PdSP,
            PdT,
            PdTN,
            PdPC,
            PdIVR,
            PdADR,
            PdMDR,
            PdIR,
            Pd0,
            PdMinus1
        };

        enum enumDBUS
        {
            NONE,
            PdFLAG,
            PdRG,
            PdSP,
            PdT,
            PdPC,
            PdIVR,
            PdADR,
            PdMDR,
            PdMDRN,
            PdIR,
            Pd0,
            PdMinus1
        };

        enum ALU
        {
            NONE,
            SBUS,
            DBUS,
            ADD,
            SUB,
            AND,
            OR,
            XOR,
            ASL,
            ASR,
            LSR,
            ROL,
            ROR,
            RLC,
            RRC,
        };

        enum enumRBUS
        {
            NONE,
            PmFLAG,
            PmFLAG3,
            PmRG,
            PmSP,
            PmT,
            PmPC,
            PmIVR,
            PmADR,
            PmMDR,
        };

        enum OTHERS
        {
            NONE,
            PLUS2SP,
            MIN2SP,
            PLUS2PC,
            A1BE0,
            A1BE1,
            PdCONDa,
            CinPdCONDa,
            PdCONDl,
            A1BVI,
            A0BVI,
            A0BPO,
            INTAMIN2SP,
            A0BEA0BI
        };

        enum SUCCESOR
        {
            STEP,
            JUMPI,
            IFACLOW,
            IFCIL,
            IFC,
            IFZ,
            IFS,
            IFV
        };

        //toate variabilele de care are nevoie secventiatorul sa mearga

        static ulong MIR = 0, MAR = 0;
        static ulong[] MPM = new ulong[115];
        static uint PC, IR, SP, FLAGS, T, ADR, MDR, IVR;
        static uint[] RG = new uint[16];
        static uint SBUS, DBUS, RBUS;
        static uint[] MEM = new uint[65536];
        static int currentState = 0;
        static int aclow, cil, cIN;
        static int INTR, INTA;
        static int bpo = 0;
        static int cALU, zALU, sALU, vALU;
        static int f, g;

        // acestea sunt variabilele folosite pentru a stoca secvente de biti din microinstructiune
        // o microinstructiune are 36 de biti in formatul nostru

        static int sursaSBUS;        // Bits 35-32
        static int sursaDBUS;        // Bits 31-28
        static int operatieALU;      // Bits 27-24
        static int destinatieRBUS;   // Bits 23-20
        static int operatieMem;      // Bits 19-18
        static int alteOperatii;     // Bits 17-14
        static int succesor;         // Bits 13-11
        static int index;            // Bits 10-8
        static int NTF;              // Bit 7
        static int microAdresaSalt;  // Bits 6-0

        static void Main(string[] args)
        {
            // initializare memorie MPM cu microinstructiuni

            InitializeMPM();

            //while-ul se va executa pana cand bitul bpo va fi deveni 1. asta inseamna ca a fost data o instructiune HALT care opreste programul.

            while (bpo == 0)
            {
                // switch-ul acesta decide ce se va face cu microinstructiunea in functie de starea curenta. intotdeauna se incepe cu starea 0

                switch (currentState)
                {
                    case 0:
                        //starea 0 doar pune in registrul MIR instructiunea aflata in vectorul MPM la adresa MAR apoi trece la starea 1

                        Console.WriteLine("\nStarea 0: MIR = MPM[MAR]");
                        MIR = MPM[MAR];
                        currentState = 1;
                        break;

                    case 1:
                        //In starea 1 mai intai se extrag din Microinstructiunea MIR campurile necesare conform codificarii instructiunii

                        Console.WriteLine("Starea 1: Enable EN1 si verificare conditie globala de ramificatie");

                        
                        sursaSBUS = (int)((MIR >> 32) & 0xF);       // Bits 35-32
                        sursaDBUS = (int)((MIR >> 28) & 0xF);       // Bits 31-28
                        operatieALU = (int)((MIR >> 24) & 0xF);     // Bits 27-24
                        destinatieRBUS = (int)((MIR >> 20) & 0xF);  // Bits 23-20
                        operatieMem = (int)((MIR >> 18) & 0x3);     // Bits 19-18
                        alteOperatii = (int)((MIR >> 14) & 0xF);    // Bits 17-14
                        succesor = (int)((MIR >> 11) & 0x7);        // Bits 13-11
                        index = (int)((MIR >> 8) & 0x7);            // Bits 10-8
                        NTF = (int)((MIR >> 7) & 0x1);              // Bit 7
                        microAdresaSalt = (int)(MIR & 0x7F);        // Bits 6-0

                        //Extragem si bitii de FLAG pentru ca poate ne trebuie la operatiile cu ALU sau salturi conditionate

                        int bvi = (int)(FLAGS >> 7) & 1;
                        int c = (int)(FLAGS >> 3) & 1;
                        int z = (int)(FLAGS >> 2) & 1;
                        int s = (int)(FLAGS >> 1) & 1;
                        int v = (int)FLAGS & 1;

                        //In functie de ce am extras de pe campul sursaSBUS facem operatia corespunzatoare => incarcam in variabila SBUS ceva

                        switch (sursaSBUS)
                        {
                            case (int)enumSBUS.NONE: break;
                            case (int)enumSBUS.PdFLAG: SBUS = FLAGS; Console.WriteLine("PdFLAG"); break;
                            case (int)enumSBUS.PdRG: SBUS = RG[(IR >> 6) & 0xF]; Console.WriteLine("PdRG"); break;
                            case (int)enumSBUS.PdSP: SBUS = SP; Console.WriteLine("PdSP"); break;
                            case (int)enumSBUS.PdT: SBUS = T; Console.WriteLine("PdT"); break;
                            case (int)enumSBUS.PdTN: SBUS = ~T; Console.WriteLine("PdTN"); break;
                            case (int)enumSBUS.PdPC: SBUS = PC; Console.WriteLine("PdPC"); break;
                            case (int)enumSBUS.PdIVR: SBUS = IVR; Console.WriteLine("PdIVR"); break;
                            case (int)enumSBUS.PdADR: SBUS = ADR; Console.WriteLine("PdADR"); break;
                            case (int)enumSBUS.PdMDR: SBUS = MDR; Console.WriteLine("PdMDR"); break;
                            case (int)enumSBUS.PdIR: SBUS = (IR & 0x00FF); Console.WriteLine("PdIR"); break;
                            case (int)enumSBUS.Pd0: SBUS = 0; Console.WriteLine("Pd0"); break;
                            case (int)enumSBUS.PdMinus1: SBUS = 0xFF; Console.WriteLine("Pd-1"); break;
                        }

                        //In functie de ce am extras de pe campul sursaDBUS facem operatia corespunzatoare => incarcam in variabila DBUS ceva

                        switch (sursaDBUS)
                        {
                            case (int)enumDBUS.NONE: break;
                            case (int)enumDBUS.PdFLAG: DBUS = FLAGS; Console.WriteLine("PdFLAG"); break;
                            case (int)enumDBUS.PdRG: DBUS = RG[(IR >> 6) & 0xF]; Console.WriteLine("PdRG"); break;
                            case (int)enumDBUS.PdSP: DBUS = SP; Console.WriteLine("PdSP"); break;
                            case (int)enumDBUS.PdT: DBUS = T; Console.WriteLine("PdT"); break;
                            case (int)enumDBUS.PdPC: DBUS = PC; Console.WriteLine("PdPC"); break;
                            case (int)enumDBUS.PdIVR: DBUS = IVR; Console.WriteLine("PdIVR"); break;
                            case (int)enumDBUS.PdADR: DBUS = ADR; Console.WriteLine("PdADR"); break;
                            case (int)enumDBUS.PdMDR: DBUS = MDR; Console.WriteLine("PdMDR"); break;
                            case (int)enumDBUS.PdMDRN: DBUS = ~MDR; Console.WriteLine("PdMDRN"); break;
                            case (int)enumDBUS.PdIR: DBUS = (IR & 0x00FF); Console.WriteLine("PdIR"); break;
                            case (int)enumDBUS.Pd0: DBUS = 0; Console.WriteLine("Pd0"); break;
                            case (int)enumDBUS.PdMinus1: DBUS = 0xFF; Console.WriteLine("PdMinus1"); break;
                        }

                        //In functie de ce am extras de pe campul operatieALU facem operatia corespunzatoare => in cele mai multe se face o operatie intre SBUS si DBUS. rezultatul se pune in RBUS

                        switch (operatieALU)
                        {
                            case (int)ALU.NONE: break;
                            case (int)ALU.SBUS: RBUS = SBUS; Console.WriteLine("SBUS"); break;
                            case (int)ALU.DBUS: RBUS = DBUS; Console.WriteLine("DBUS"); break;
                            case (int)ALU.ADD:
                                RBUS = (SBUS + DBUS);
                                if (SBUS > (UInt16.MaxValue - DBUS)) { cALU = 1; }
                                if (SBUS > (Int16.MaxValue - DBUS)) { vALU = 1; }
                                Console.WriteLine("ADD");
                                break;
                            case (int)ALU.SUB:
                                if (DBUS > SBUS) { cALU = 1; }
                                uint result = SBUS - DBUS;
                                if ((SBUS > 0 && DBUS < 0 && result < 0) || (SBUS < 0 && DBUS > 0 && result > 0)) { vALU = 1; }
                                RBUS = SBUS - DBUS;
                                Console.WriteLine("SUB");
                                break;
                            case (int)ALU.AND: RBUS = (SBUS & DBUS); Console.WriteLine("AND"); break;
                            case (int)ALU.OR: RBUS = (SBUS | DBUS); Console.WriteLine("OR"); break;
                            case (int)ALU.XOR: RBUS = (SBUS ^ DBUS); Console.WriteLine("XOR"); break;
                            case (int)ALU.ASL: RBUS = (SBUS >> (int)DBUS); Console.WriteLine("ASL"); break;
                            case (int)ALU.ASR: RBUS = (SBUS << (int)DBUS); Console.WriteLine("ASR"); break;
                            case (int)ALU.LSR:
                                uint x = DBUS;
                                while (x > 0)
                                {
                                    RBUS = RBUS >> 1;
                                    RBUS &= 0x7FFF;
                                    x--;
                                }
                                Console.WriteLine("LSR");
                                break;
                            case (int)ALU.ROL: RBUS = ((SBUS << (int)DBUS) | (SBUS >> (int)(-DBUS & 16))); Console.WriteLine("ROL"); break;
                            case (int)ALU.ROR: RBUS = ((SBUS >> (int)DBUS) | (SBUS << (int)(-DBUS & 16))); Console.WriteLine("ROR"); break;
                            case (int)ALU.RLC:
                                uint y = DBUS;
                                int carry1;
                                RBUS = SBUS;
                                while (y > 0)
                                {
                                    carry1 = cALU;
                                    cALU = (int)(RBUS & 0x8000);
                                    RBUS = (SBUS << 1) | ((uint)carry1 >> 15);
                                    y--;
                                }
                                Console.WriteLine("RLC");
                                break;
                            case (int)ALU.RRC:
                                uint w = DBUS;
                                int carry2;
                                RBUS = SBUS;
                                while (w > 0)
                                {
                                    carry2 = cALU;
                                    cALU = (int)RBUS & 0x0001;
                                    RBUS = ((SBUS >> 1) | ((uint)carry2 << 15));
                                    w--;
                                }
                                Console.WriteLine("RRC");
                                break;
                        }

                        //In functie de ce valoare are acum RBUS se pot schimba bitii de zero si sign

                        if (RBUS == 0)
                        {
                            zALU = 1;
                        }
                        else
                        {
                            zALU = 0;
                        }

                        if (RBUS >= 0)
                        {
                            sALU = 0;
                        }
                        else
                        {
                            sALU = 1;
                        }

                        //In functie de ce am extras de pe campul sursaRBUS facem operatia corespunzatoare => punem RBUS intr-o alta variabila

                        switch (destinatieRBUS)
                        {
                            case (int)enumRBUS.NONE: break;
                            case (int)enumRBUS.PmFLAG: FLAGS = RBUS; Console.WriteLine("PmFLAG"); break;
                            case (int)enumRBUS.PmFLAG3: FLAGS = (ushort)(FLAGS & (RBUS & 0x3)); Console.WriteLine("PmFLAG3"); break;
                            case (int)enumRBUS.PmRG: RG[IR & 0xF] = RBUS; Console.WriteLine("PmRG"); break;
                            case (int)enumRBUS.PmSP: SP = RBUS; Console.WriteLine("PmSP"); break;
                            case (int)enumRBUS.PmT: T = RBUS; Console.WriteLine("PmT"); break;
                            case (int)enumRBUS.PmPC: PC = RBUS; Console.WriteLine("PmPC"); break;
                            case (int)enumRBUS.PmIVR: IVR = RBUS; Console.WriteLine("PmIVR"); break;
                            case (int)enumRBUS.PmADR: ADR = RBUS; Console.WriteLine("PmADR"); break;
                            case (int)enumRBUS.PmMDR: MDR = RBUS; Console.WriteLine("PmMDR"); break;
                        }

                        //In functie de ce am extras de pe campul alteOperatii facem operatia corespunzatoare => astea sunt operatii care schimba alte variabile din program (PC, SP, FLAGS, aclow, cil etc.)

                        switch (alteOperatii)
                        {
                            case (int)OTHERS.NONE: break;
                            case (int)OTHERS.PLUS2SP: SP = (SP + 2); Console.WriteLine("+2SP"); break;
                            case (int)OTHERS.MIN2SP: SP = (SP - 2); Console.WriteLine("-2SP"); break;
                            case (int)OTHERS.PLUS2PC: PC = (PC + 2); Console.WriteLine("+2PC"); break;
                            case (int)OTHERS.A1BE0: aclow = 1; Console.WriteLine("A(1)BE0"); break;
                            case (int)OTHERS.A1BE1: cil = 1; Console.WriteLine("A(1)BE1"); break;
                            case (int)OTHERS.PdCONDa:
                                FLAGS |= (ushort)(cALU << 3);
                                FLAGS |= (ushort)(zALU << 2);
                                FLAGS |= (ushort)(sALU << 1);
                                FLAGS |= (ushort)vALU;
                                Console.WriteLine("PdCONDa");
                                break;
                            case (int)OTHERS.CinPdCONDa:
                                cIN = 1;
                                FLAGS |= (ushort)(cALU << 3);
                                FLAGS |= (ushort)(zALU << 2);
                                FLAGS |= (ushort)(sALU << 1);
                                Console.WriteLine("CinPdCONDa");
                                break;
                            case (int)OTHERS.PdCONDl:
                                FLAGS |= (ushort)vALU;
                                FLAGS |= (ushort)(zALU << 2);
                                FLAGS |= (ushort)(sALU << 1);
                                Console.WriteLine("PdCONDl");
                                break;
                            case (int)OTHERS.A1BVI: bvi = (ushort)1; Console.WriteLine("A(1)BVI"); break;
                            case (int)OTHERS.A0BVI: bvi = 0; Console.WriteLine("A(0)BVI"); break;
                            case (int)OTHERS.A0BPO: bpo = 1; Console.WriteLine("A(0)BPO"); break;
                            case (int)OTHERS.INTAMIN2SP: INTA = 1; SP = (SP - 2); Console.WriteLine("INTA, -2SP"); break;
                            case (int)OTHERS.A0BEA0BI: aclow = 0; cil = 0; bvi = 0; Console.WriteLine("A(0)BE, A(0)BI"); break;
                        }

                        //In functie de ce am extras de pe campul succesor facem operatia corespunzatoare => se modifica variabilele f si g pentru a determia cum continua programul

                        switch (succesor)
                        {
                            case (int)SUCCESOR.STEP: f = NTF; g = 0; Console.WriteLine("STEP"); break;
                            case (int)SUCCESOR.JUMPI: f = NTF == 0 ? 1 : 0; g = 1; Console.WriteLine("JUMPI"); break;
                            case (int)SUCCESOR.IFACLOW: f = aclow; g = f ^ NTF; Console.WriteLine("IFACLOW"); break;
                            case (int)SUCCESOR.IFCIL: f = cil; g = cil ^ NTF; Console.WriteLine("IFCIL"); break;
                            case (int)SUCCESOR.IFC: f = c; g = c ^ NTF; Console.WriteLine("IFC"); break;
                            case (int)SUCCESOR.IFZ: f = z; g = z ^ NTF; Console.WriteLine("IFZ"); break;
                            case (int)SUCCESOR.IFS: f = s; g = s ^ NTF; Console.WriteLine("IFS"); break;
                            case (int)SUCCESOR.IFV: f = v; g = v ^ NTF; Console.WriteLine("IFV"); break;
                        }

                        // daca conditia globala de ramificatie (adica g) este 1 atunci se face salt la adresa data de microAdresaDeSalt si index care au fost amandoua extrase la inceput din microinstructiune
                        // daca g este 0 atunci se trece la urmatoarea microinstructiune din MPM

                        if (g == 1)
                        {
                            Console.WriteLine("g este adevarata => Load MAR si salt la adresa");
                            MAR = (uint)(microAdresaSalt + index);
                        }
                        else
                        {
                            Console.WriteLine("g este fals => MAR++");
                            MAR++;
                        }

                        //In functie de ce am extras de pe campul operatieMem se decide daca se trece in starea 0 sau 2

                        if (operatieMem == (int)MEMORIE.NONE)
                        {
                            Console.WriteLine("Operatia nu modifica memoria");
                            currentState = 0;
                        }
                        else
                        {
                            Console.WriteLine("Operatia modifica memoria");
                            currentState = 2;
                        }

                        break;

                    case 2:
                        // starea asta nu face nimic. doar trece la starea 3

                        Console.WriteLine("Starea 2: Nicio operatie, trecem in starea 3");
                        currentState = 3;
                        break;

                    case 3:
                        // starea 3 este doar pentru instructiunile cu acces la memorie

                        Console.Write("Starea 3: Operatie cu acces la memorie ");
                        operatieMem = (int)((MIR >> 18) & 0x3);

                        // in functie de ce am extras in campul operatieMem atunci se face o diferita operatie pe Memorie: IFCH, READ sau WRITE. apoi se trece la starea 0

                        switch (operatieMem)
                        {
                            case (int)MEMORIE.IFCH: IR = MEM[ADR]; Console.WriteLine("IFCH"); break;
                            case (int)MEMORIE.RD: MDR = MEM[ADR]; Console.WriteLine("READ"); break;
                            case (int)MEMORIE.WR: MEM[ADR] = MDR; Console.WriteLine("WRITE"); break;
                        }

                        currentState = 0;
                        break;
                }
            }
        }

        static void InitializeMPM()
        {
            // functia pune in MPM cateva microinstructiuni cu care sa poata lucra. la instructiunea halt programul se va termina intotdeauna.

            MPM[0] = 0b0100_10000011_10010001_10000100_00000000; // Add 
            MPM[1] = 0b0101_01100111_00110100_10000000_00000000; // instructiune random => PdTs negat, PdIVRd, XOR, PmRG, IFCH, -2SP,STEP
            MPM[2] = 0b0010_01010010_00100001_01000000_00000000; // instructiune random
            MPM[3] = 0b0000_00000000_00000010_11000000_00000000; // Halt
        }
    }
}