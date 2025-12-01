using ArchsVsDinosServer;
using ArchsVsDinosServer.BusinessLogic;
using ArchsVsDinosServer.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ProfileManagementTests
{
    [TestClass]
    public class ProfileManagementTestBase : BaseTestClass
    {
        protected Mock<IValidationHelper> mockValidationHelper;
        protected Mock<ISecurityHelper> mockSecurityHelper;

        [TestInitialize]
        public void BaseSetup()
        {
            base.BaseSetup();
            
            mockValidationHelper = new Mock<IValidationHelper>();
            mockSecurityHelper = new Mock<ISecurityHelper>();

           
        }

    }
}
