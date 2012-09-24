﻿using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Samba.Domain.Models.Accounts;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;

namespace Samba.Modules.PaymentModule.Tests
{
    [TestFixture]
    public class PaymentOrderSelectorTests
    {
        private static OrderSelector SetupOrderSelector()
        {
            var ticket = SetupTicket();
            var orderSelector = new OrderSelector();
            orderSelector.UpdateTicket(ticket);
            return orderSelector;
        }

        private static Ticket SetupTicket()
        {
            var tost = new MenuItem("Tost") { Id = 1 };
            var hamburger = new MenuItem("Hamburger") { Id = 2 };
            var ticket = Ticket.Create(Department.Default, Account.Null, 1, new List<CalculationTemplate>());
            var order = ticket.AddOrder(AccountTransactionTemplate.Default, "Emre", tost, new MenuItemPortion { Price = 5, Name = "Adet" }, "", null);
            order.Quantity = 2;
            order.PortionCount = 2;
            ticket.AddOrder(AccountTransactionTemplate.Default, "Emre", tost, new MenuItemPortion { Price = 5, Name = "Adet" }, "", null);
            ticket.AddOrder(AccountTransactionTemplate.Default, "Emre", hamburger, new MenuItemPortion { Price = 7, Name = "Adet" }, "", null);
            ticket.AddOrder(AccountTransactionTemplate.Default, "Emre", hamburger, new MenuItemPortion { Price = 6, Name = "Adet" }, "", null);
            return ticket;
        }

        [Test]
        public void CanUpdatesTicketCorrectly()
        {
            var ticket = new Ticket();
            var orderSelector = new OrderSelector();
            orderSelector.UpdateTicket(ticket);
            Assert.AreEqual(ticket, orderSelector.SelectedTicket);
        }

        [Test]
        public void DoesSelectorCountMatch()
        {
            var orderSelector = SetupOrderSelector();
            Assert.AreEqual(3, orderSelector.Selectors.Count);
        }

        [Test]
        public void DoesSelectorTotalsMatch()
        {
            var orderSelector = SetupOrderSelector();
            Assert.AreEqual(3, orderSelector.Selectors[0].Quantity);
            Assert.AreEqual(5, orderSelector.Selectors.Sum(x => x.Quantity));
            Assert.AreEqual(28, orderSelector.Selectors.Sum(x => x.TotalPrice));
        }

        [Test]
        public void DoesTicketDiscountsWorks()
        {
            var ticket = SetupTicket();
            var calculationTemplate = new CalculationTemplate
                                          {
                                              AccountTransactionTemplate = AccountTransactionTemplate.Default,
                                              DecreaseAmount = true
                                          };
            ticket.AddCalculation(calculationTemplate, 10);
            var orderSelector = new OrderSelector();
            orderSelector.UpdateTicket(ticket);
            Assert.AreEqual(28 - 2.8, orderSelector.Selectors.Sum(x => x.TotalPrice));
        }

        [Test]
        public void CanSelectItems()
        {
            var orderSelector = SetupOrderSelector();
            orderSelector.Select(1, 5);
            Assert.AreEqual(5, orderSelector.SelectedTotal);
            orderSelector.Select(2, 6);
            Assert.AreEqual(11, orderSelector.SelectedTotal);
            Assert.AreEqual(2, orderSelector.Selectors.Sum(x => x.SelectedQuantity));
        }

        [Test]
        public void CanUpdatePaidTicketItems()
        {
            var ticket = SetupTicket();
            var orderSelector = new OrderSelector();
            orderSelector.UpdateTicket(ticket);
            orderSelector.Select(1, 5);
            orderSelector.Select(2, 6);
            orderSelector.PersistTicket();
            Assert.AreEqual(2, ticket.PaidItems.Sum(x => x.Quantity));
        }

        [Test]
        public void UpdatesSelectorDescriptionCorrectly()
        {
            var orderSelector = SetupOrderSelector();
            Assert.AreEqual("Tost.Adet", orderSelector.Selectors[0].Description);
            Assert.AreEqual("Hamburger", orderSelector.Selectors[2].Description);
        }

        [Test]
        public void CanCalculateExchangeRate()
        {
            var orderSelector = SetupOrderSelector();
            orderSelector.UpdateExchangeRate(1.5m);
            orderSelector.Select(1, 5);
            orderSelector.Select(2, 6);
            Assert.AreEqual(decimal.Round(11 / 1.5m, 2), orderSelector.SelectedTotal);
            orderSelector.UpdateExchangeRate(2m);
            Assert.AreEqual(decimal.Round(11 / 2m, 2), orderSelector.SelectedTotal);
        }

        [Test]
        public void CanUpdateHalfPaidTicket()
        {
            var ticket = SetupTicket();
            var orderSelector = new OrderSelector();
            orderSelector.UpdateTicket(ticket);
            orderSelector.Select(1, 5);
            orderSelector.Select(2, 6);
            orderSelector.PersistTicket();

            orderSelector = new OrderSelector();
            orderSelector.UpdateTicket(ticket);
            Assert.AreEqual(28 - 11, orderSelector.RemainingTotal);
        }

        [Test]
        public void CanCancelInvalidSelections()
        {
            var orderSelector = SetupOrderSelector();
            orderSelector.Select(1, 5);
            Assert.AreEqual(5, orderSelector.SelectedTotal);
            orderSelector.Select(1, 5);
            Assert.AreEqual(10, orderSelector.SelectedTotal);
            orderSelector.Select(1, 5);
            Assert.AreEqual(15, orderSelector.SelectedTotal);
            orderSelector.Select(1, 5);
            Assert.AreEqual(15, orderSelector.SelectedTotal);
        }

        [Test]
        public void CanClearSelectedItems()
        {
            var orderSelector = SetupOrderSelector();
            orderSelector.Select(1, 5);
            orderSelector.Select(1, 5);
            Assert.AreEqual(10, orderSelector.SelectedTotal);
            orderSelector.ClearSelection();
            Assert.AreEqual(0, orderSelector.SelectedTotal);
            orderSelector.Select(1, 5);
            orderSelector.Select(1, 5);
            orderSelector.PersistSelectedItems();
            orderSelector.Select(1, 5);
            Assert.AreEqual(5, orderSelector.SelectedTotal);
            orderSelector.ClearSelection();
            Assert.AreEqual(0, orderSelector.SelectedTotal);
        }

        [Test]
        public void CanHandleMultipleUpdates()
        {
            var ticket = SetupTicket();
            var orderSelector = new OrderSelector();
            orderSelector.UpdateTicket(ticket);
            Assert.AreEqual(28, orderSelector.RemainingTotal);
            orderSelector.UpdateTicket(ticket);
            Assert.AreEqual(28, orderSelector.RemainingTotal);
        }

        [Test]
        public void CanCalculateRemainingTotal()
        {
            var orderSelector = SetupOrderSelector();
            orderSelector.Select(1, 5);
            orderSelector.PersistSelectedItems();
            Assert.AreEqual(23, orderSelector.RemainingTotal);
        }
    }
}