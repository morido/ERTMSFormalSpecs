// ------------------------------------------------------------------------------
// -- Copyright ERTMS Solutions
// -- Licensed under the EUPL V.1.1
// -- http://joinup.ec.europa.eu/software/page/eupl/licence-eupl
// --
// -- This file is part of ERTMSFormalSpec software and documentation
// --
// --  ERTMSFormalSpec is free software: you can redistribute it and/or modify
// --  it under the terms of the EUPL General Public License, v.1.1
// --
// -- ERTMSFormalSpec is distributed in the hope that it will be useful,
// -- but WITHOUT ANY WARRANTY; without even the implied warranty of
// -- MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// --
// ------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GUI.DataDictionaryView
{
    public class NameSpaceVariablesTreeNode : ModelElementTreeNode<DataDictionary.Types.NameSpace>
    {
        /// <summary>
        /// The editor for message variables
        /// </summary>
        private class ItemEditor : NamedEditor
        {
            /// <summary>
            /// Constructor
            /// </summary>
            public ItemEditor()
                : base()
            {
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="item"></param>
        /// <param name="name"></param>
        public NameSpaceVariablesTreeNode(DataDictionary.Types.NameSpace item, bool buildSubNodes)
            : base(item, buildSubNodes, "Variables", true)
        {
        }

        /// <summary>
        /// Builds the subnodes of this node
        /// </summary>
        /// <param name="buildSubNodes">Indicates that subnodes of the nodes built should also </param>
        public override void BuildSubNodes(bool buildSubNodes)
        {
            base.BuildSubNodes(buildSubNodes);

            foreach (DataDictionary.Variables.Variable variable in Item.Variables)
            {
                Nodes.Add(new VariableTreeNode(variable, buildSubNodes, new HashSet<DataDictionary.Types.Type>()));
            }
            SortSubNodes();
        }

        /// <summary>
        /// Creates the editor for this tree node
        /// </summary>
        /// <returns></returns>
        protected override Editor createEditor()
        {
            return new ItemEditor();
        }

        public void AddHandler(object sender, EventArgs args)
        {
            DataDictionary.Variables.Variable variable = (DataDictionary.Variables.Variable)DataDictionary.Generated.acceptor.getFactory().createVariable();
            variable.Name = "<Variable" + (GetNodeCount(false) + 1) + ">";
            AddVariable(variable);
        }

        /// <summary>
        /// Adds a variable in the corresponding namespace
        /// </summary>
        /// <param name="variable"></param>
        public VariableTreeNode AddVariable(DataDictionary.Variables.Variable variable)
        {
            // Ensure that variables always have a type
            if (variable.Type == null)
            {
                variable.Type = variable.EFSSystem.BoolType;
            }

            Item.appendVariables(variable);
            VariableTreeNode retVal = new VariableTreeNode(variable, true, new HashSet<DataDictionary.Types.Type>());
            Nodes.Add(retVal);
            SortSubNodes();

            return retVal;
        }

        /// <summary>
        /// The menu items for this tree node
        /// </summary>
        /// <returns></returns>
        protected override List<MenuItem> GetMenuItems()
        {
            List<MenuItem> retVal = new List<MenuItem>();

            retVal.Add(new MenuItem("Add", new EventHandler(AddHandler)));

            return retVal;
        }

        /// <summary>
        /// Accepts drop of a tree node, in a drag & drop operation
        /// </summary>
        /// <param name="SourceNode"></param>
        public override void AcceptDrop(BaseTreeNode SourceNode)
        {
            base.AcceptDrop(SourceNode);

            if (SourceNode is VariableTreeNode)
            {
                VariableTreeNode variableTreeNode = SourceNode as VariableTreeNode;
                DataDictionary.Variables.Variable variable = variableTreeNode.Item;

                variableTreeNode.Delete();
                AddVariable(variable);
            }
            else if (SourceNode is SpecificationView.ParagraphTreeNode)
            {
                SpecificationView.ParagraphTreeNode node = SourceNode as SpecificationView.ParagraphTreeNode;
                DataDictionary.Specification.Paragraph paragaph = node.Item;

                DataDictionary.Variables.Variable variable = (DataDictionary.Variables.Variable)DataDictionary.Generated.acceptor.getFactory().createVariable();
                variable.Name = paragaph.Name;

                DataDictionary.ReqRef reqRef = (DataDictionary.ReqRef)DataDictionary.Generated.acceptor.getFactory().createReqRef();
                reqRef.Name = paragaph.FullId;
                variable.appendRequirements(reqRef);
                AddVariable(variable);
            }
        }

        /// <summary>
        /// Update counts according to the selected folder
        /// </summary>
        /// <param name="displayStatistics">Indicates that statistics should be displayed in the MDI window</param>
        public override void SelectionChanged(bool displayStatistics)
        {
            base.SelectionChanged(false);

            GUIUtils.MDIWindow.SetStatus(Item.Variables.Count + (Item.Variables.Count > 1 ? " variables " : " variable ") + "selected.");
        }
    }
}
