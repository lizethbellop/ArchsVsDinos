using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Entity;
using Contracts.DTO;

namespace UnitTest
{
    [TestClass]
    public class RegisterTest
    {

        [TestInitialize]
        public void Setup()
        {
            Register.verificationCodes.Clear();
        }


        [TestMethod]
        public void TestCorrectSendEmailRegister()
        {
            //Arrange
            Register register = new Register();
            string email = "test@example.com";

            //Act
            bool result = register.SendEmailRegister(email);

            //Assert
            Assert.IsTrue(result, "Email should be sent successfully");
        }

        [TestMethod]
        public void TestIncorrectSendEmailRegister()
        {
            //Arrange
            Register register = new Register();
            string email = "correoIncorrecto_ejemploouluk.com";

            //Act
            bool result = register.SendEmailRegister(email);

            //Assert
            Assert.IsFalse(result, "Email should be invalid");
        }

        [TestMethod]
        public void TestCorrectCheckCode()
        {
            //Arrange 
            Register register = new Register();
            string email = "test@example.com";
            string code = "A7HC3N9K";
                Register.verificationCodes.Add(new VerificationCode
                {
                    Email = email,
                    Code = code,
                    Expiration = DateTime.Now.AddMinutes(10)
                });

            //Act
            bool result = register.CheckCode(email, code);

            //Assert
            Assert.IsTrue(result, "Correct code");
        }

        [TestMethod]
        public void TestIncorrectCheckCode()
        {
            //Arrange 
            Register register = new Register();
            string email = "test@example.com";
            string code = "CORRECTCODE";

            Register.verificationCodes.Add(new VerificationCode
            {
                Email = email,
                Code = code,
                Expiration = DateTime.Now.AddMinutes(10)
            });

            //Act
            bool result = register.CheckCode(email, "incorrectCode");

            //Assert
            Assert.IsFalse(result, "Incorrect code");
        }

    }
}
