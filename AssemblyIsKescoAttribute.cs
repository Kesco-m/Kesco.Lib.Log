using System;
using System.Runtime.InteropServices;


namespace Kesco.Lib.Log
{
	/// <summary>
	/// Атрибут сборки, означающий что проект поддерживается программистами Атэк-груп
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false), ComVisible(true)]
	public sealed class AssemblyIsKescoAttribute : Attribute
	{
		private bool isKesco;
		/// <summary>
		/// Флаг означает, что проект локальный и поддержка исходного кода проекта осуществляется программистами компании
		/// </summary>
		public bool IsKesco { get { return this.isKesco; } }

		/// <summary>
		/// Конструктор, определяющий представляет сборка интерес с точки зрения отладки или нет
		/// </summary>
		/// <param name="kesco">true - сборка холдинга и ее методы будут анализироваться системой разбора ошибок</param>
		public AssemblyIsKescoAttribute( bool kesco )
		{
			this.isKesco = kesco;
		}

		/// <summary>
		/// Конструктор по-умолчанию. Сбора холдинга.
		/// </summary>
		public AssemblyIsKescoAttribute() : this( true )
		{
		}
	}
}
