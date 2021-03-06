﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit.Extensions;
using Xunit;
using Ploeh.Hyprlinkr;
using System.Linq.Expressions;
using Ploeh.Hyprlinkr.UnitTest.Controllers;
using Ploeh.AutoFixture.Xunit;
using System.Net.Http;
using System.Web.Http;
using Moq;
using System.Reflection;
using Ploeh.AutoFixture.Idioms;
using Moq.Protected;

namespace Ploeh.Hyprlinkr.UnitTest
{
    public class RouteLinkerTests
    {
        [Theory, AutoHypData]
        public void ConstructorHasAppropriateGuards(GuardClauseAssertion assertion)
        {
            assertion.Verify(typeof(RouteLinker).GetConstructors());
        }

        [Theory, AutoHypData]
        public void SutIsRouteLinker(RouteLinker sut)
        {
            Assert.IsAssignableFrom<IResourceLinker>(sut);
        }

        [Theory, AutoHypData]
        public void GetNullThrows(RouteLinker sut)
        {
            Assert.Throws<ArgumentNullException>(() =>
                sut.GetUri<Version>(null));
        }

        [Theory, AutoHypData]
        public void GetUriFromInvalidExpressionThrows(RouteLinker sut)
        {
            Expression<Action<object>> expressionWhichIsNotAMethodCall =
                _ => new object();
            Assert.Throws<ArgumentException>(() =>
                sut.GetUri<object>(expressionWhichIsNotAMethodCall));
        }

        [Theory, AutoHypData]
        public void GetFooRouteForDefaultGetMethodReturnsCorrectResult(
            [Frozen]HttpRequestMessage request,
            RouteLinker sut)
        {
            request.AddDefaultRoute();

            Uri actual = sut.GetUri<FooController>(r => r.GetDefault());

            var baseUri = request.RequestUri.GetLeftPart(UriPartial.Authority);
            var expected = new Uri(new Uri(baseUri), "api/foo");
            Assert.Equal(expected, actual);
        }

        [Theory, AutoHypData]
        public void GetBarRouteForDefaultGetMethodReturnsCorrectResult(
            [Frozen]HttpRequestMessage request,
            RouteLinker sut)
        {
            request.AddDefaultRoute();

            Uri actual = sut.GetUri<BarController>(r => r.GetDefault());

            var baseUri = request.RequestUri.GetLeftPart(UriPartial.Authority);
            var expected = new Uri(new Uri(baseUri), "api/bar");
            Assert.Equal(expected, actual);
        }

        [Theory, AutoHypData]
        public void GetUriForGetMethodWithParameters(
            [Frozen]HttpRequestMessage request,
            RouteLinker sut,
            int id)
        {
            request.AddDefaultRoute();

            var actual = sut.GetUri<FooController>(r => r.GetById(id));

            var baseUri = request.RequestUri.GetLeftPart(UriPartial.Authority);
            var expected = new Uri(new Uri(baseUri), "api/foo/"+ id);
            Assert.Equal(expected, actual);
        }

        [Theory, AutoHypData]
        public void GetUriForGetMethodWithNamedParametersReturnsCorrectResult(
            [Frozen]HttpRequestMessage request,
            RouteLinker sut,
            int ploeh,
            string fnaah)
        {
            request.AddDefaultRoute();

            var actual = sut.GetUri<FooController>(r =>
                r.GetWithPloehAndFnaah(ploeh, fnaah));

            var baseUri = request.RequestUri.GetLeftPart(UriPartial.Authority);
            var expected = 
                new Uri(
                    new Uri(baseUri),
                    "api/foo?ploeh=" + ploeh + "&fnaah=" + fnaah);
            Assert.Equal(expected, actual);
        }

        [Theory, AutoHypData]
        public void GetUriWithCustomRouteAndDispatcherReturnsCorrectResult(
            [Frozen]HttpRequestMessage request,
            [Frozen]Mock<IRouteDispatcher> dispatcherStub,
            string routeName,
            [Greedy]RouteLinker sut,
            int ploeh,
            string fnaah)
        {
            // Arrange
            request.AddDefaultRoute();
            request.AddRoute(
                name: routeName,
                routeTemplate: "foo/{ploeh}/{fnaah}",
                defaults: new { });

            var method = Reflect<FooController>
                .GetMethod(c => c.GetWithPloehAndFnaah(ploeh, fnaah));
            dispatcherStub
                .Setup(d =>
                    d.Dispatch(method, It.IsAny<IDictionary<string, object>>()))
                .Returns((MethodInfo _, IDictionary<string, object> routeValues) =>
                    new Rouple(routeName, routeValues));

            // Act
            var actual = sut.GetUri<FooController>(r =>
                r.GetWithPloehAndFnaah(ploeh, fnaah));

            // Assert
            var baseUri = request.RequestUri.GetLeftPart(UriPartial.Authority);
            var expected =
                new Uri(
                    new Uri(baseUri),
                    "foo/" + ploeh + "/" + fnaah);
            Assert.Equal(expected, actual);
        }

        [Theory, AutoHypData]
        public void SutIsDisposable(RouteLinker sut)
        {
            Assert.IsAssignableFrom<IDisposable>(sut);
        }

        [Theory, AutoHypData]
        public void DisposeDisposesRequest(Mock<HttpRequestMessage> requestMock)
        {
            var sut = new RouteLinker(requestMock.Object);
            sut.Dispose();
            requestMock.Protected().Verify("Dispose", Times.Once(), true);
        }

        [Theory, AutoHypData]
        public void GivenModestConstructorRequestIsCorrect(
            [Frozen]HttpRequestMessage expected,
            [Modest]RouteLinker sut)
        {
            Assert.Equal<HttpRequestMessage>(expected, sut.Request);
        }

        [Theory, AutoHypData]
        public void GivenGreedyConstructorRequestIsCorrect(
            [Frozen]HttpRequestMessage expected,
            [Greedy]RouteLinker sut)
        {
            Assert.Equal<HttpRequestMessage>(expected, sut.Request);
        }

        [Theory, AutoHypData]
        public void GivenGreedyConstructorDispatcherIsCorrect(
            [Frozen]IRouteDispatcher expected,
            [Greedy]RouteLinker sut)
        {
            Assert.Equal<IRouteDispatcher>(expected, sut.RouteDispatcher);
        }

        [Theory, AutoHypData]
        public void GetFooRouteForDefaultGetMethodFromIndexedUriReturnsCorrectResult(
            [Frozen]HttpRequestMessage request,
            RouteLinker sut,
            string currentId)
        {
            request.RequestUri = new Uri(request.RequestUri, "api/foo/" + currentId);
            request.AddRoute(
                name: "API Default",
                routeTemplate: "api/{controller}/{id}",
                defaults: new
                {
                    controller = "Home",
                    id = RouteParameter.Optional
                });

            Uri actual = sut.GetUri<FooController>(r => r.GetDefault());

            var baseUri = request.RequestUri.GetLeftPart(UriPartial.Authority);
            var expected = new Uri(new Uri(baseUri), "api/foo");
            Assert.Equal(expected, actual);
        }
    }
}
