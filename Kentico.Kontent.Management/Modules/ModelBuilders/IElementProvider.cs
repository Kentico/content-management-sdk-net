using System;

namespace Kentico.Kontent.Management.Modules.ModelBuilders
{
    /// <summary>
    /// Defines the contract for mapping Kentico Kontent content types
    /// and its' elements to CLR types properties
    /// </summary>
    public interface IElementProvider
    {
        /// <summary>
        /// Returns codename of an element by its id
        /// </summary>
        /// <param name="type">Type that contains an element with the given id</param>
        /// <param name="id">Id of the element</param>
        /// <returns></returns>
        string GetElementCodenameById(Type type, string id);

        /// <summary>
        /// Returns id of an element by its codename
        /// </summary>
        /// <param name="type">Type that contains an element with the given codename</param>
        /// <param name="name">Codename of the element</param>
        /// <returns></returns>
        string GetElementIdByCodename(Type type, string name);
    }
}
