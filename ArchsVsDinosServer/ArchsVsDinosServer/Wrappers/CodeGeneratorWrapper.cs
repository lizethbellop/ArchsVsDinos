using ArchsVsDinosServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.Interfaces;

namespace ArchsVsDinosServer.Wrappers
{
    public class CodeGeneratorWrapper : ICodeGenerator
    {
        public string GenerateVerificationCode()
        {
            return CodeGenerator.GenerateVerificationCode();
        }

        public string GenerateMatchCode()
        {
            return CodeGenerator.GenerateMatchCode();
        }

        public string GenerateCode(int length)
        {
            return CodeGenerator.GenerateCode(length);
        }
    }
}
