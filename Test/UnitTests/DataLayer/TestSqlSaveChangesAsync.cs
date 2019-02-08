﻿// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using DataLayer.EfCode;
using DataLayer.NoSqlCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Cosmos.Storage.Internal;
using Test.Helpers;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.DataLayer
{
    public class TestSqlSaveChangesAsync
    {
        [Fact]
        public async Task TestSaveChangesAsyncAddNoSqlOk()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var builder = new DbContextOptionsBuilder<NoSqlDbContext>()
                .UseCosmos(
                    config["endpoint"],
                    config["authKey"],
                    nameof(TestSqlSaveChanges));

            var options = SqliteInMemory.CreateOptions<SqlDbContext>();
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                await sqlContext.Database.EnsureCreatedAsync();
                await noSqlContext.Database.EnsureCreatedAsync();

                //ATTEMPT
                var book = DddEfTestData.CreateDummyBookTwoAuthorsTwoReviews();
                sqlContext.Add(book);
                await sqlContext.SaveChangesAsync();

                //VERIFY
                sqlContext.Books.Count().ShouldEqual(1);
                var noSqlBook = noSqlContext.Books.SingleOrDefault(p => p.BookId == book.BookId);
                noSqlBook.ShouldNotBeNull();
                noSqlBook.AuthorsOrdered.ShouldEqual("Author1, Author2");
                noSqlBook.ReviewsCount.ShouldEqual(2);
            }
        }

        //--------------------------------------------------------------
        //error situations


        [Fact]
        public async Task TestSaveChangesAsyncUpdatesNoSqlFail()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var builder = new DbContextOptionsBuilder<NoSqlDbContext>()
                .UseCosmos(
                    config["endpoint"],
                    config["authKey"],
                    "UNKNOWNDATABASENAME");

            var options = SqliteInMemory.CreateOptions<SqlDbContext>();
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                await sqlContext.Database.EnsureCreatedAsync();

                //ATTEMPT
                var book = DddEfTestData.CreateDummyBookOneAuthor();
                sqlContext.Add(book);
                var ex = await Assert.ThrowsAsync<HttpException>(async () => await sqlContext.SaveChangesAsync());

                //VERIFY
                sqlContext.Books.Count().ShouldEqual(0);
            }
        }

        [Fact]
        public async Task TestSaveChangesDeleteNoSqlOk()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var builder = new DbContextOptionsBuilder<NoSqlDbContext>()
                .UseCosmos(
                    config["endpoint"],
                    config["authKey"],
                    nameof(TestSqlSaveChanges));

            var options = SqliteInMemory.CreateOptions<SqlDbContext>();
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                sqlContext.Database.EnsureCreated();
                noSqlContext.Database.EnsureCreated();
                var book = DddEfTestData.CreateDummyBookTwoAuthorsTwoReviews();
                sqlContext.Add(book);
                await sqlContext.SaveChangesAsync();

                //ATTEMPT
                sqlContext.Remove(book);
                await sqlContext.SaveChangesAsync();

                //VERIFY
                sqlContext.Books.Count().ShouldEqual(0);
                var noSqlBook = noSqlContext.Books.SingleOrDefault(p => p.BookId == book.BookId);
                noSqlBook.ShouldBeNull();
            }
        }

        [Fact]
        public async Task TestSaveChangesDirectUpdatesNoSqlOk()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var builder = new DbContextOptionsBuilder<NoSqlDbContext>()
                .UseCosmos(
                    config["endpoint"],
                    config["authKey"],
                    nameof(TestSqlSaveChanges));

            var options = SqliteInMemory.CreateOptions<SqlDbContext>();
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                sqlContext.Database.EnsureCreated();
                noSqlContext.Database.EnsureCreated();
                var book = DddEfTestData.CreateDummyBookTwoAuthorsTwoReviews();
                sqlContext.Add(book);
                await sqlContext.SaveChangesAsync();
            }
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                //ATTEMPT
                var book = sqlContext.Books.Single();
                book.PublishedOn = DddEfTestData.DummyBookStartDate.AddDays(1);
                await sqlContext.SaveChangesAsync();

                //VERIFY
                sqlContext.Books.Count().ShouldEqual(1);
                var noSqlBook = noSqlContext.Books.Single(p => p.BookId == book.BookId);
                noSqlBook.PublishedOn.ShouldEqual(DddEfTestData.DummyBookStartDate.AddDays(1));
            }
        }

        [Fact]
        public async Task TestSaveChangesIndirectUpdatesNoSqlOk()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var builder = new DbContextOptionsBuilder<NoSqlDbContext>()
                .UseCosmos(
                    config["endpoint"],
                    config["authKey"],
                    nameof(TestSqlSaveChanges));

            var options = SqliteInMemory.CreateOptions<SqlDbContext>();
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                sqlContext.Database.EnsureCreated();
                noSqlContext.Database.EnsureCreated();
                var book = DddEfTestData.CreateDummyBookTwoAuthorsTwoReviews();
                sqlContext.Add(book);
                await sqlContext.SaveChangesAsync();
            }
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                //ATTEMPT
                var book = sqlContext.Books.Single();
                book.AddReview(5, "xxx","yyy", sqlContext);
                await sqlContext.SaveChangesAsync();

                //VERIFY
                sqlContext.Books.Count().ShouldEqual(1);
                var noSqlBook = noSqlContext.Books.Single(p => p.BookId == book.BookId);
                noSqlBook.ReviewsCount.ShouldEqual(3);
            }
        }

        [Fact]
        public async Task TestSaveChangesSoftDeleteNoSqlOk()
        {
            //SETUP
            var config = AppSettings.GetConfiguration();
            var builder = new DbContextOptionsBuilder<NoSqlDbContext>()
                .UseCosmos(
                    config["endpoint"],
                    config["authKey"],
                    nameof(TestSqlSaveChanges));

            var options = SqliteInMemory.CreateOptions<SqlDbContext>();
            using (var noSqlContext = new NoSqlDbContext(builder.Options))
            using (var sqlContext = new SqlDbContext(options, new NoSqlBookUpdater(noSqlContext)))
            {
                sqlContext.Database.EnsureCreated();
                noSqlContext.Database.EnsureCreated();
                var book = DddEfTestData.CreateDummyBookTwoAuthorsTwoReviews();
                sqlContext.Add(book);
                await sqlContext.SaveChangesAsync();

                //ATTEMPT
                book.SoftDeleted = true;
                await sqlContext.SaveChangesAsync();

                //VERIFY
                sqlContext.Books.Count().ShouldEqual(0);
                sqlContext.Books.IgnoreQueryFilters().Count().ShouldEqual(1);
                var noSqlBook = noSqlContext.Books.SingleOrDefault(p => p.BookId == book.BookId);
                noSqlBook.ShouldBeNull();
            }
        }
    }
}