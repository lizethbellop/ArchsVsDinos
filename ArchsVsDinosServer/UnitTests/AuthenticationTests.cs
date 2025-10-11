using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class LoginTest
    {
        private Mock<ISecurityHelper> mockSecurityHelper;
        private Mock<IValidationHelper> mockValidationHelper;
        private Mock<ILoggerHelper> mockLoggerHelper;
        private Mock<IDbContext> mockDbContext;
        private Authentication authentication;

        [TestInitialize]
        public void Setup()
        {
            mockSecurityHelper = new Mock<ISecurityHelper>();
            mockValidationHelper = new Mock<IValidationHelper>();
            mockLoggerHelper = new Mock<ILoggerHelper>();
            mockDbContext = new Mock<IDbContext>();
            
            authentication = new Authentication(mockSecurityHelper.Object, mockValidationHelper.Object, 
                mockLoggerHelper.Object, () => mockDbContext.Object);
        }


        [TestMethod]
        public void TestLoginEmptyFields()
        {
            //Arrange
            string username = "";
            string password = "";

            mockValidationHelper.Setup(v => v.IsEmpty(username)).Returns(true);
            mockValidationHelper.Setup(v => v.IsEmpty(password)).Returns(true);

            //Act
            var result = authentication.Login(username, password);

            //Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Campos requeridos", result.Message);

            mockValidationHelper.Verify(v => v.IsEmpty(It.IsAny<string>()), Times.AtLeastOnce());
        }

    }
}
