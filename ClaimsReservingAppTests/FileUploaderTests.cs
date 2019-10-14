using ClaimsReservingApp.Controllers;
using ClaimsReservingApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ClaimsReservingAppTests
{
    public class FileUploaderTests
    {
        [Fact]
        public void Valid_File_Should_Return_True()
        {
            //Arrange
            var fileMock = new Mock<IFormFile>();
            //Setup mock file using a memory stream
            var content = "Comp, 1992, 1992, 110.0";
            var fileName = "validFile.csv";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            var fileUploadController = new FileUploadController(Mock.Of<IWebHostEnvironment>());
            var validFile = fileMock.Object;

            //Act
            bool isValid = fileUploadController.CheckIfValid(validFile);

            //Assert
            Assert.True(isValid);
        }

        [Fact]
        public void Invalid_File_Should_Return_False()
        {
            //Arrange
            var fileMock = new Mock<IFormFile>();
            //Setup mock file using a memory stream
            var content = "";
            var fileName = "EmptyFile.txt";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            var fileUploadController = new FileUploadController(Mock.Of<IWebHostEnvironment>());
            var invalidFile = fileMock.Object;

            //Act
            bool isValid = fileUploadController.CheckIfValid(invalidFile);

            //Assert
            Assert.False(isValid);
        }

        [Fact]
        public void File_Should_Be_Mapped_To_PaymentRecords()
        {
            //Arrange
            var fileMock = new Mock<IFormFile>();
            //Setup mock file using a memory stream
            var content = "Product, Origin Year, Development Year, Incremental Value" + Environment.NewLine + "Comp, 1992, 1992, 110.0";
            var fileName = "ExampleFile.txt";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            var fileUploadController = new FileUploadController(Mock.Of<IWebHostEnvironment>());
            var exampleFile = fileMock.Object;
            List<PaymentRecord> expectedPaymentRecords = new List<PaymentRecord>()
            {
                new PaymentRecord{Product = "Comp", OriginYear = 1992, DevelopmentYear = 1992, IncrementalValue = 110M}
            };

            //Act
            List<PaymentRecord> actualPaymentRecords = fileUploadController.getFileRecords(exampleFile);

            //Assert
            Assert.True(expectedPaymentRecords[0].Product == actualPaymentRecords[0].Product &&
                        expectedPaymentRecords[0].OriginYear == actualPaymentRecords[0].OriginYear &&
                        expectedPaymentRecords[0].DevelopmentYear == actualPaymentRecords[0].DevelopmentYear &&
                        expectedPaymentRecords[0].IncrementalValue == actualPaymentRecords[0].IncrementalValue);
        }

        [Fact]
        public void Empty_IncrementalValue_Should_Be_Mapped_To_Null()
        {
            //Arrange
            var fileMock = new Mock<IFormFile>();
            //Setup mock file using a memory stream
            var content = "Product, Origin Year, Development Year, Incremental Value" + Environment.NewLine + "Comp, 1992, 1992,";
            var fileName = "ExampleFile.txt";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            var fileUploadController = new FileUploadController(Mock.Of<IWebHostEnvironment>());
            var exampleFile = fileMock.Object;
            List<PaymentRecord> expectedPaymentRecords = new List<PaymentRecord>()
            {
                new PaymentRecord{Product = "Comp", OriginYear = 1992, DevelopmentYear = 1992, IncrementalValue = null}
            };

            //Act
            List<PaymentRecord> actualPaymentRecords = fileUploadController.getFileRecords(exampleFile);

            //Assert
            Assert.True(expectedPaymentRecords[0].IncrementalValue == actualPaymentRecords[0].IncrementalValue);
        }

        [Fact]
        public void PaymentRecord_Should_Output_Correct_CumulativeData()
        {
            //Arrange
            var fileUploadController = new FileUploadController(Mock.Of<IWebHostEnvironment>());
            List<PaymentRecord> paymentRecord = new List<PaymentRecord>()
            {
                new PaymentRecord{Product = "Comp", OriginYear = 1992, DevelopmentYear = 1992, IncrementalValue = 110M},
                new PaymentRecord{Product = "Comp", OriginYear = 1992, DevelopmentYear = 1993, IncrementalValue = 170M},
                new PaymentRecord{Product = "Comp", OriginYear = 1993, DevelopmentYear = 1993, IncrementalValue = 200M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1990, DevelopmentYear = 1990, IncrementalValue = 45.2M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1990, DevelopmentYear = 1991, IncrementalValue = 64.8M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1990, DevelopmentYear = 1993, IncrementalValue = 37.0M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1991, DevelopmentYear = 1991, IncrementalValue = 50.0M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1991, DevelopmentYear = 1992, IncrementalValue = 75.0M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1991, DevelopmentYear = 1993, IncrementalValue = 25.0M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1992, DevelopmentYear = 1992, IncrementalValue = 55.0M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1992, DevelopmentYear = 1993, IncrementalValue = 85.0M},
                new PaymentRecord{Product = "Non-Comp", OriginYear = 1993, DevelopmentYear = 1993, IncrementalValue = 100.0M}
            };
            List<string> expectedProcessedRecords = new List<string>()
            {
                "1990, 4",
                "Comp, 0, 0, 0, 0, 0, 0, 0, 110, 280, 200",
                "Non-Comp, 45.2, 110, 110, 147, 50, 125, 150, 55, 140, 100"
            };

            //Act
            List<string> actualProcessedRecords = fileUploadController.calculateCumulativeData(paymentRecord);

            //Assert
            Assert.True(expectedProcessedRecords[0] == actualProcessedRecords[0] &&
                        expectedProcessedRecords[1] == actualProcessedRecords[1] &&
                        expectedProcessedRecords[2] == actualProcessedRecords[2]);
        }
    }
}
