using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using ArchsVsDinosServer.Utils;
using Contracts.DTO;
using Contracts.DTO.Response;
using Contracts.DTO.Result_Codes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICodeGenerator = ArchsVsDinosServer.Interfaces.ICodeGenerator;

namespace UnitTest
{
    [TestClass]
    public class RegisterTest : BaseTestClass
    {

        private Register register;
        private Mock<IEmailService> mockEmailService;
        private Mock<ICodeGenerator> mockCodeGenerator;
        private Mock<IVerificationCodeManager> mockCodeManager;
        private Mock<DbSet<Configuration>> mockConfigurationSet;
        protected Mock<IValidationHelper> mockValidationHelper;
        protected Mock<ISecurityHelper> mockSecurityHelper;

        [TestInitialize]
        public void Setup()
        {
            base.BaseSetup();
            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockEmailService = new Mock<IEmailService>();
            mockCodeGenerator = new Mock<ICodeGenerator>();
            mockCodeManager = new Mock<IVerificationCodeManager>();
            mockConfigurationSet = new Mock<DbSet<Configuration>>();

            RegisterServiceDependencies dependencies = new RegisterServiceDependencies
            {
                securityHelper = mockSecurityHelper.Object,
                loggerHelper = mockLoggerHelper.Object,
                emailService = mockEmailService.Object,
                codeGenerator = mockCodeGenerator.Object,
                codeManager = mockCodeManager.Object,
                contextFactory = () => mockDbContext.Object
            };

            register = new Register(dependencies);
        }


        [TestMethod]
        public void TestValidateUsernameAndNicknameBothAvailable()
        {
            string username = "newuser";
            string nickname = "newnick";

            SetupMockUserSet(new List<UserAccount>());

            RegisterResponse result = register.ValidateUsernameAndNicknameResult(username, nickname);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public void TestValidateUsernameAndNicknameUsernameExists()
        {
            string username = "existinguser";
            string nickname = "newnick";

            var existingUser = new UserAccount { username = "existinguser" };
            SetupMockUserSet(new List<UserAccount> { existingUser });

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_UsernameExists
            };

            RegisterResponse result = register.ValidateUsernameAndNicknameResult(username, nickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestValidateUsernameAndNicknameNicknameExists()
        {
            string username = "newuser";
            string nickname = "existingnick";

            var existingUser = new UserAccount { nickname = "existingnick" };
            SetupMockUserSet(new List<UserAccount> { existingUser });

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_NicknameExists
            };

            RegisterResponse result = register.ValidateUsernameAndNicknameResult(username, nickname);

            Assert.AreEqual(expectedResult, result);
        }

        public void TestValidateUsernameAndNicknameBothExist()
        {
            string username = "existinguser";
            string nickname = "existingnick";

            var existingUser = new UserAccount { username = "existinguser", nickname = "existingnick" };
            SetupMockUserSet(new List<UserAccount> { existingUser });

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_BothExists
            };

            RegisterResponse result = register.ValidateUsernameAndNicknameResult(username, nickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestValidateUsernameAndNicknameDatabaseError()
        {
            string username = "testuser";
            string nickname = "testnick";

            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_DatabaseError
            };

            RegisterResponse result = register.ValidateUsernameAndNicknameResult(username, nickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestValidateUsernameAndNicknameUnexpectedError()
        {
            string username = "testuser";
            string nickname = "testnick";

            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_UnexpectedError
            };

            RegisterResponse result = register.ValidateUsernameAndNicknameResult(username, nickname);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestSendEmailRegisterSuccess()
        {
            string email = "test@example.com";
            string generatedCode = "123456";

            mockCodeGenerator.Setup(c => c.GenerateVerificationCode()).Returns(generatedCode);
            mockEmailService.Setup(e => e.SendVerificationEmail(email, generatedCode)).Returns(true);

            bool result = register.SendEmailRegister(email);

            Assert.IsTrue(result);
            mockCodeManager.Verify(c => c.AddCode(email, generatedCode, It.IsAny<DateTime>()), Times.Once);
        }

        [TestMethod]
        public void TestSendEmailRegisterEmailServiceFails()
        {
            string email = "test@example.com";
            string generatedCode = "123456";

            mockCodeGenerator.Setup(c => c.GenerateVerificationCode()).Returns(generatedCode);
            mockEmailService.Setup(e => e.SendVerificationEmail(email, generatedCode)).Returns(false);

            bool result = register.SendEmailRegister(email);

            Assert.IsFalse(result);
            mockCodeManager.Verify(c => c.AddCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
        }

        [TestMethod]
        public void TestSendEmailRegisterExceptionThrown()
        {
            string email = "test@example.com";

            mockCodeGenerator.Setup(c => c.GenerateVerificationCode()).Throws(new Exception("Unexpected error"));

            bool result = register.SendEmailRegister(email);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestSendEmailRegisterCallsCodeGenerator()
        {
            string email = "test@example.com";
            string generatedCode = "123456";

            mockCodeGenerator.Setup(c => c.GenerateVerificationCode()).Returns(generatedCode);
            mockEmailService.Setup(e => e.SendVerificationEmail(email, generatedCode)).Returns(true);

            register.SendEmailRegister(email);

            mockCodeGenerator.Verify(c => c.GenerateVerificationCode(), Times.Once);
        }

        [TestMethod]
        public void TestSendEmailRegisterCallsEmailServiceWithCorrectParameters()
        {
            string email = "test@example.com";
            string generatedCode = "123456";

            mockCodeGenerator.Setup(c => c.GenerateVerificationCode()).Returns(generatedCode);
            mockEmailService.Setup(e => e.SendVerificationEmail(email, generatedCode)).Returns(true);

            register.SendEmailRegister(email);

            mockEmailService.Verify(e => e.SendVerificationEmail(email, generatedCode), Times.Once);
        }

        [TestMethod]
        public void TestRegisterUserInvalidCode()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "testuser",
                Nickname = "testnick"
            };
            string code = "wrongcode";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(false);

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_InvalidCode
            };

            RegisterResponse result = register.RegisterUser(userAccountDTO, code);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRegisterUserUsernameExistss()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "existinguser",
                Nickname = "newnick"
            };
            string code = "123456";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);

            var existingUser = new UserAccount { username = "existinguser" };
            SetupMockUserSet(new List<UserAccount> { existingUser });

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_UsernameExists
            };

            RegisterResponse result = register.RegisterUser(userAccountDTO, code);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRegisterUser_NicknameExists_ReturnsNicknameExists()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "existingnick"
            };
            string code = "123456";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);

            var existingUser = new UserAccount { nickname = "existingnick" };
            SetupMockUserSet(new List<UserAccount> { existingUser });

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_NicknameExists
            };

            RegisterResponse result = register.RegisterUser(userAccountDTO, code);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRegisterUserSuccess()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";
            string hashedPassword = "hashedPassword";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(userAccountDTO.Password)).Returns(hashedPassword);
            SetupMockUserSet(new List<UserAccount>());
            SetupMockPlayerSet(new List<Player>());
            SetupMockConfigurationSet(new List<Configuration>());

            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = true,
                ResultCode = RegisterResultCode.Register_Success
            };

            RegisterResponse result = register.RegisterUser(userAccountDTO, code);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRegisterUserCallsHashPassword()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";
            string hashedPassword = "hashedPassword";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(userAccountDTO.Password)).Returns(hashedPassword);
            SetupMockUserSet(new List<UserAccount>());
            SetupMockPlayerSet(new List<Player>());
            SetupMockConfigurationSet(new List<Configuration>());
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            register.RegisterUser(userAccountDTO, code);

            mockSecurityHelper.Verify(s => s.HashPassword(userAccountDTO.Password), Times.Once);
        }

        [TestMethod]
        public void TestRegisterUserCallsSaveChangesThreeTimes()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";
            string hashedPassword = "hashedPassword";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(userAccountDTO.Password)).Returns(hashedPassword);
            SetupMockUserSet(new List<UserAccount>());
            SetupMockPlayerSet(new List<Player>());
            SetupMockConfigurationSet(new List<Configuration>());
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            register.RegisterUser(userAccountDTO, code);

            mockDbContext.Verify(c => c.SaveChanges(), Times.Exactly(3));
        }

        [TestMethod]
        public void TestRegisterUserDatabaseError()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockDbContext.Setup(c => c.UserAccount).Throws(new EntityException("Database error"));

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_DatabaseError
            };

            RegisterResponse result = register.RegisterUser(userAccountDTO, code);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRegisterUserUnexpectedError()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockDbContext.Setup(c => c.UserAccount).Throws(new Exception("Unexpected error"));

            RegisterResponse expectedResult = new RegisterResponse
            {
                Success = false,
                ResultCode = RegisterResultCode.Register_UnexpectedError
            };

            RegisterResponse result = register.RegisterUser(userAccountDTO, code);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestRegisterUserAddsPlayerToContext()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";
            string hashedPassword = "hashedPassword";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(userAccountDTO.Password)).Returns(hashedPassword);
            SetupMockUserSet(new List<UserAccount>());
            SetupMockPlayerSet(new List<Player>());
            SetupMockConfigurationSet(new List<Configuration>());
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            register.RegisterUser(userAccountDTO, code);

            mockPlayerSet.Verify(p => p.Add(It.IsAny<Player>()), Times.Once);
        }

        [TestMethod]
        public void TestRegisterUserAddsConfigurationToContext()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";
            string hashedPassword = "hashedPassword";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(userAccountDTO.Password)).Returns(hashedPassword);
            SetupMockUserSet(new List<UserAccount>());
            SetupMockPlayerSet(new List<Player>());
            SetupMockConfigurationSet(new List<Configuration>());
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            register.RegisterUser(userAccountDTO, code);

            mockConfigurationSet.Verify(c => c.Add(It.IsAny<Configuration>()), Times.Once);
        }

        [TestMethod]
        public void TestRegisterUserAddsUserAccountToContext()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "newuser",
                Nickname = "newnick"
            };
            string code = "123456";
            string hashedPassword = "hashedPassword";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(true);
            mockSecurityHelper.Setup(s => s.HashPassword(userAccountDTO.Password)).Returns(hashedPassword);
            SetupMockUserSet(new List<UserAccount>());
            SetupMockPlayerSet(new List<Player>());
            SetupMockConfigurationSet(new List<Configuration>());
            mockDbContext.Setup(c => c.SaveChanges()).Returns(1);

            register.RegisterUser(userAccountDTO, code);

            mockUserSet.Verify(u => u.Add(It.IsAny<UserAccount>()), Times.Once);
        }

        [TestMethod]
        public void TestRegisterUserValidatesCodeBeforeProcessing()
        {
            var userAccountDTO = new UserAccountDTO
            {
                Email = "test@example.com",
                Password = "password123",
                Name = "Test User",
                Username = "testuser",
                Nickname = "testnick"
            };
            string code = "123456";

            mockCodeManager.Setup(c => c.ValidateCode(userAccountDTO.Email, code)).Returns(false);

            register.RegisterUser(userAccountDTO, code);

            mockCodeManager.Verify(c => c.ValidateCode(userAccountDTO.Email, code), Times.Once);
            mockUserSet.Verify(u => u.Add(It.IsAny<UserAccount>()), Times.Never);
        }

        protected void SetupMockConfigurationSet(List<Configuration> configurations)
        {
            var queryableConfigurations = configurations.AsQueryable();
            mockConfigurationSet.As<IQueryable<Configuration>>().Setup(m => m.Provider).Returns(queryableConfigurations.Provider);
            mockConfigurationSet.As<IQueryable<Configuration>>().Setup(m => m.Expression).Returns(queryableConfigurations.Expression);
            mockConfigurationSet.As<IQueryable<Configuration>>().Setup(m => m.ElementType).Returns(queryableConfigurations.ElementType);
            mockConfigurationSet.As<IQueryable<Configuration>>().Setup(m => m.GetEnumerator()).Returns(queryableConfigurations.GetEnumerator());
            mockDbContext.Setup(c => c.Configuration).Returns(mockConfigurationSet.Object);
        }
    }


}
