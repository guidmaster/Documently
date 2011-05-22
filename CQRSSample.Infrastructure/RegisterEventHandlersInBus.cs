﻿using System;
using System.Linq;
using System.Reflection;
using Castle.Windsor;
using CQRSSample.Domain.Events;
using CQRSSample.ReadModel;

namespace CQRSSample.Infrastructure
{
	public class RegisterEventHandlersInBus
	{
		private static MethodInfo _createPublishActionMethod;
		private static MethodInfo _registerMethod;

		public static void BootStrap(IWindsorContainer container)
		{
			new RegisterEventHandlersInBus().RegisterEventHandlers(container);
		}

		private void RegisterEventHandlers(IWindsorContainer container)
		{
			var bus = container.Resolve<IBus>();

			_createPublishActionMethod = GetType().GetMethod("CreatePublishAction");
			_registerMethod = bus.GetType().GetMethod("RegisterHandler");

			var handlers = typeof (CustomerListView)
				.Assembly
				.GetExportedTypes()
				.Where(x => x.GetInterfaces().Any(y => y.IsGenericType && y.GetGenericTypeDefinition() == typeof (HandlesEvent<>)))
				.ToList();

			foreach (var handlerType in handlers)
			{
				var handleEventTypes = handlerType.GetInterfaces().Where(
					x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof (HandlesEvent<>));

				foreach (var handleEventType in handleEventTypes)
				{
					var eventHandler = container.Resolve(handleEventType);
					var action = CreateTheProperAction(handleEventType, eventHandler);
					RegisterTheCreatedAction(bus, handleEventType, action);
				}
			}
		}

		public Action<TMessage> CreatePublishAction<TMessage, TMessageHandler>(TMessageHandler messageHandler)
			where TMessage : DomainEvent
			where TMessageHandler : HandlesEvent<TMessage>
		{
			return messageHandler.Handle;
		}

		private void RegisterTheCreatedAction(IBus bus, Type handleEventType, object action)
		{
			_registerMethod.MakeGenericMethod(handleEventType).Invoke(bus, new[] {action});
		}

		private object CreateTheProperAction(Type eventType, object eventHandler)
		{
			return _createPublishActionMethod.MakeGenericMethod(eventType, eventHandler.GetType()).Invoke(this,
			                                                                                              new[] {eventHandler});
		}
	}
}