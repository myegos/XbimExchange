﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.CobieLiteUk;
using Xbim.Ifc4.Interfaces;


namespace XbimExchanger.IfcToCOBieLiteUK
{
    public class MappingSystemViaIfcPropertyToSystem : XbimMappings<IModel, List<Facility>, string, IIfcPropertySet, Xbim.CobieLiteUk.System>
    {
        protected override Xbim.CobieLiteUk.System Mapping(IIfcPropertySet pSet, Xbim.CobieLiteUk.System target)
        {
            var helper = ((IfcToCOBieLiteUkExchanger)Exchanger).Helper;

            //Add Assets
            var systemAssignments = helper.GetSystemAssignments(pSet);

            var ifcObjectDefinitions = systemAssignments as IList<IIfcObjectDefinition> ?? systemAssignments.ToList();
            string name = string.Empty;
            if (ifcObjectDefinitions.Any())
            {
                name = GetSystemName(helper, ifcObjectDefinitions);

                target.Components = new List<AssetKey>();
                foreach (var ifcObjectDefinition in ifcObjectDefinitions)
                {

                    var assetKey = new AssetKey { Name = ifcObjectDefinition.Name };
                    if (!target.Components.Contains(assetKey))
                    {
                        target.Components.Add(assetKey);
                    }
                    
                }
            }
            target.ExternalEntity = helper.ExternalEntityName(pSet);
            target.ExternalId = helper.ExternalEntityIdentity(pSet);
            target.ExternalSystem = helper.ExternalSystemName(pSet);
            target.Name = string.IsNullOrEmpty(name) ? "Unknown" : name;
            target.Description = string.IsNullOrEmpty(pSet.Description) ? name : pSet.Description.ToString();
            target.CreatedBy = helper.GetCreatedBy(pSet);
            target.CreatedOn = helper.GetCreatedOn(pSet);
            target.Categories = helper.GetCategories(pSet);

            //Attributes, no attributes from PSet as Pset is the attributes, assume that component attributes are extracted by each component anyway
            //target.Attributes = helper.GetAttributes(pSet);

            //Documents
            var docsMappings = Exchanger.GetOrCreateMappings<MappingIfcDocumentSelectToDocument>();
            helper.AddDocuments(docsMappings, target, pSet);

            //TODO:
            //System Issues
           
            return target;
        }


        /// <summary>
        /// Get system name from a IfcObjectDefinition
        /// </summary>
        /// <param name="helper">CoBieLiteUkHelper</param>
        /// <param name="ifcObjects">List of IfcObjectDefinitions</param>
        /// <returns></returns>
        private static string GetSystemName(CoBieLiteUkHelper helper, IList<IIfcObjectDefinition> ifcObjects )
        {   string name = string.Empty;
            var propMaps = helper.GetPropMap("SystemMaps").ToList();
            if (propMaps.Count() > 0)
            {
                propMaps = propMaps.Concat(propMaps.ConvertAll(s => s.Split(new Char[] { '.' })[0] + ".System Classification")).ToList();
                var propNameOrder = propMaps.Select(s => s.Split(new Char[] { '.' })[1]).Distinct().ToList();
                    
                foreach (var obj in ifcObjects)
                {
                    var atts = helper.GetAttributesObj(obj);
                    if (atts != null)
                    {
                        //get propery values as system name
                        var value = atts.Properties.Where(prop => propMaps.Contains(prop.Key)) //properties which match mappings
                                                   .Select(prop => prop.Value).OfType<IIfcPropertySingleValue>()
                                                   .Where(propSV => propSV.NominalValue != null && !string.IsNullOrEmpty(propSV.NominalValue.ToString())) //has a value
                                                   .Select(propSV => propSV.Name.ToString() + ":" + propSV.NominalValue.ToString())
                                                   .Distinct().OrderBy(s => propNameOrder.IndexOf(s.Split(new Char[] { ':' })[0]));
                        if (value.Any())
                        {
                            name = value.First();//string.Join(":", value);
                        }
                        else //no name so try proprty names
                        {
                            //Try and get the property names as system name
                            value = atts.Properties.Where(prop => propMaps.Contains(prop.Key)) //properties which match mappings
                                                   .Select(prop => prop.Value).OfType<IIfcPropertySingleValue>()
                                                   .Where(propSV => propSV.Name != null && !string.IsNullOrEmpty(propSV.Name.ToString()))
                                                   .Select(prop => prop.Name.ToString())
                                                   .Distinct().OrderBy(s => propNameOrder.IndexOf(s));
                            if (value.Any())
                            {
                                name = string.Join(":", value);
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(name)) break; //exit loop if name can be constructed
                }
            }
            return name;
        }


        public override Xbim.CobieLiteUk.System CreateTargetObject()
        {
            return new Xbim.CobieLiteUk.System();
        }
    }
}
