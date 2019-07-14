using System;

namespace project.Messages
{
	public class ImplementationAttribute : Attribute
	{
		public Type Binder;
		
		public ImplementationAttribute(Type binder)
		{
			Binder = binder;
		}
	}
}