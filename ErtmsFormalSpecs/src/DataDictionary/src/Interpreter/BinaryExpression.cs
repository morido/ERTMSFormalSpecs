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

namespace DataDictionary.Interpreter
{
    public class BinaryExpression : Expression
    {
        /// <summary>
        /// The left expression of this expression
        /// </summary>
        public Expression Left { get; private set; }

        /// <summary>
        /// The available operators
        /// </summary>
        public enum OPERATOR { EXP, MULT, DIV, ADD, SUB, EQUAL, NOT_EQUAL, IN, NOT_IN, LESS, LESS_OR_EQUAL, GREATER, GREATER_OR_EQUAL, AND, OR, DOT, UNDEF };

        public static OPERATOR[] OperatorsLevel0 = { OPERATOR.OR, };
        public static OPERATOR[] OperatorsLevel1 = { OPERATOR.AND, };
        public static OPERATOR[] OperatorsLevel2 = { OPERATOR.EQUAL, OPERATOR.NOT_EQUAL, OPERATOR.IN, OPERATOR.NOT_IN, OPERATOR.LESS_OR_EQUAL, OPERATOR.GREATER_OR_EQUAL, OPERATOR.LESS, OPERATOR.GREATER, };
        public static OPERATOR[] OperatorsLevel3 = { OPERATOR.ADD, OPERATOR.SUB };
        public static OPERATOR[] OperatorsLevel4 = { OPERATOR.MULT, OPERATOR.DIV };
        public static OPERATOR[] OperatorsLevel5 = { OPERATOR.EXP };
        public static OPERATOR[][] OperatorsByLevel = { OperatorsLevel0, OperatorsLevel1, OperatorsLevel2, OperatorsLevel3, OperatorsLevel4, OperatorsLevel5 };

        /// <summary>
        /// The available operators
        /// </summary>
        public static OPERATOR[] Operators =
        {
            OPERATOR.OR, OPERATOR.AND,
            OPERATOR.EQUAL, OPERATOR.NOT_EQUAL, OPERATOR.IN, OPERATOR.NOT_IN, OPERATOR.LESS_OR_EQUAL, OPERATOR.GREATER_OR_EQUAL, OPERATOR.LESS, OPERATOR.GREATER,
            OPERATOR.ADD, OPERATOR.SUB,
            OPERATOR.MULT, OPERATOR.DIV,
            OPERATOR.EXP,
            OPERATOR.DOT,
        };

        /// <summary>
        /// The corresponding operator images
        /// </summary>
        public static string[] OperatorsImages =
        {
            "OR", "AND",
            "==", "!=", "in", "not in", "<=", ">=", "<", ">",
            "+", "-",
            "*", "/",
            "^",
            ".",
        };

        /// <summary>
        /// The operation for this expression
        /// </summary>
        public OPERATOR Operation { get; private set; }

        /// <summary>
        /// The right expression of this expression
        /// </summary>
        public Expression Right { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="left"></param>
        /// <param name="op"></param>
        /// <param name="right"></param>
        public BinaryExpression(ModelElement root, Expression left, OPERATOR op, Expression right)
            : base(root)
        {
            Left = left;
            Left.Enclosing = this;

            Operation = op;
            Right = right;
            Right.Enclosing = this;
        }

        /// <summary>
        /// Performs the semantic analysis of the expression
        /// </summary>
        /// <param name="context"></param>
        /// <paraparam name="expectation">Indicates the kind of element we are looking for</paraparam>
        /// <returns>True if semantic analysis should be continued</returns>
        public override bool SemanticAnalysis(InterpretationContext context, AcceptableChoice expectation)
        {
            bool retVal = base.SemanticAnalysis(context, expectation);

            if (retVal)
            {
                Left.SemanticAnalysis(context, IsLeftSide);
                Right.SemanticAnalysis(context, IsTypedElement);
            }

            return retVal;
        }

        /// <summary>
        /// Provides the type of this expression
        /// </summary>
        /// <returns></returns>
        public override Types.Type GetExpressionType()
        {
            Types.Type retVal = null;

            Types.Type leftType = Left.GetExpressionType();
            if (leftType == null)
            {
                AddError("Cannot determine expression type (1) for " + Left.ToString());
            }
            else
            {
                Types.Type rightType = Right.GetExpressionType();
                if (rightType == null)
                {
                    AddError("Cannot determine expression type (2) for " + Right.ToString());
                }
                else
                {
                    switch (Operation)
                    {
                        case OPERATOR.EXP:
                        case OPERATOR.MULT:
                        case OPERATOR.DIV:
                        case OPERATOR.ADD:
                        case OPERATOR.SUB:
                            if (leftType.Match(rightType))
                            {
                                if (leftType is Types.IntegerType || leftType is Types.DoubleType)
                                {
                                    retVal = rightType;
                                }
                                else
                                {
                                    retVal = leftType;
                                }
                            }
                            else
                            {
                                retVal = leftType.CombineType(rightType, Operation);
                            }

                            break;

                        case OPERATOR.AND:
                        case OPERATOR.OR:
                            if (leftType == EFSSystem.BoolType && rightType == EFSSystem.BoolType)
                            {
                                retVal = EFSSystem.BoolType;
                            }
                            break;

                        case OPERATOR.EQUAL:
                        case OPERATOR.NOT_EQUAL:
                        case OPERATOR.LESS:
                        case OPERATOR.LESS_OR_EQUAL:
                        case OPERATOR.GREATER:
                        case OPERATOR.GREATER_OR_EQUAL:
                            if (leftType.Match(rightType) || rightType.Match(leftType))
                            {
                                retVal = EFSSystem.BoolType;
                            }
                            break;

                        case OPERATOR.IN:
                        case OPERATOR.NOT_IN:
                            Types.Collection collection = rightType as Types.Collection;
                            if (collection != null)
                            {
                                if (collection.Type == null)
                                {
                                    retVal = EFSSystem.BoolType;
                                }
                                else if (collection.Type == leftType)
                                {
                                    retVal = EFSSystem.BoolType;
                                }
                            }
                            else
                            {
                                Types.StateMachine stateMachine = rightType as Types.StateMachine;
                                if (stateMachine != null && leftType.Match(stateMachine))
                                {
                                    retVal = EFSSystem.BoolType;
                                }
                            }
                            break;

                        case OPERATOR.UNDEF:
                            break;
                    }
                }
            }

            return retVal;
        }

        /// <summary>
        /// Provides the value associated to this Term
        /// </summary>
        /// <param name="instance">The instance on which the value is computed</param>
        /// <param name="globalFind">Indicates that the search should be performed globally</param>
        /// <returns></returns>
        public override Values.IValue GetValue(InterpretationContext context)
        {
            Values.IValue retVal = null;
            ExplanationPart previous = SetupExplanation();

            Values.IValue leftValue = null;
            Values.IValue rightValue = null;
            try
            {
                leftValue = Left.GetValue(context);
                if (leftValue != null)
                {
                    switch (Operation)
                    {
                        case OPERATOR.EXP:
                        case OPERATOR.MULT:
                        case OPERATOR.ADD:
                        case OPERATOR.SUB:
                        case OPERATOR.DIV:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = leftValue.Type.PerformArithmericOperation(context, leftValue, Operation, rightValue);
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.AND:
                            {
                                if (leftValue.Type == EFSSystem.BoolType)
                                {
                                    Values.BoolValue lb = leftValue as Values.BoolValue;

                                    if (lb.Val)
                                    {
                                        rightValue = Right.GetValue(context);
                                        if (rightValue != null)
                                        {
                                            if (rightValue.Type == EFSSystem.BoolType)
                                            {
                                                retVal = rightValue as Values.BoolValue;
                                            }
                                            else
                                            {
                                                AddError("Cannot apply an operator " + Operation.ToString() + " on a variable of type " + rightValue.GetType());
                                            }
                                        }
                                        else
                                        {
                                            AddError("Error while computing value for " + Right.ToString());
                                        }
                                    }
                                    else
                                    {
                                        retVal = lb;
                                    }
                                }
                                else
                                {
                                    AddError("Cannot apply an operator " + Operation.ToString() + " on a variable of type " + leftValue.GetType());
                                }
                            }
                            break;

                        case OPERATOR.OR:
                            {
                                if (leftValue.Type == EFSSystem.BoolType)
                                {
                                    Values.BoolValue lb = leftValue as Values.BoolValue;

                                    if (!lb.Val)
                                    {
                                        rightValue = Right.GetValue(context);
                                        if (rightValue != null)
                                        {
                                            if (rightValue.Type == EFSSystem.BoolType)
                                            {
                                                retVal = rightValue as Values.BoolValue;
                                            }
                                            else
                                            {
                                                AddError("Cannot apply an operator " + Operation.ToString() + " on a variable of type " + rightValue.GetType());
                                            }
                                        }
                                        else
                                        {
                                            AddError("Error while computing value for " + Right.ToString());
                                        }
                                    }
                                    else
                                    {
                                        retVal = lb;
                                    }
                                }
                                else
                                {
                                    AddError("Cannot apply an operator " + Operation.ToString() + " on a variable of type " + leftValue.GetType());
                                }
                            }
                            break;

                        case OPERATOR.LESS:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(leftValue.Type.Less(leftValue, rightValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.LESS_OR_EQUAL:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(leftValue.Type.CompareForEquality(leftValue, rightValue) || leftValue.Type.Less(leftValue, rightValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.GREATER:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(leftValue.Type.Greater(leftValue, rightValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.GREATER_OR_EQUAL:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(leftValue.Type.CompareForEquality(leftValue, rightValue) || leftValue.Type.Greater(leftValue, rightValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.EQUAL:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(leftValue.Type.CompareForEquality(leftValue, rightValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.NOT_EQUAL:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(!leftValue.Type.CompareForEquality(leftValue, rightValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.IN:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(rightValue.Type.Contains(rightValue, leftValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;

                        case OPERATOR.NOT_IN:
                            {
                                rightValue = Right.GetValue(context);
                                if (rightValue != null)
                                {
                                    retVal = EFSSystem.GetBoolean(!rightValue.Type.Contains(rightValue, leftValue));
                                }
                                else
                                {
                                    AddError("Error while computing value for " + Right.ToString());
                                }
                            }
                            break;
                    }
                }
                else
                {
                    AddError("Error while computing value for " + Left.ToString());
                }
            }
            catch (Exception e)
            {
                AddError(e.Message);
            }

            if (explain)
            {
                CompleteExplanation(previous, ToString() + " = " + retVal.ToString());
            }

            return retVal;
        }

        /// <summary>
        /// Fills the list of variables used by this expression
        /// </summary>
        /// <context></context>
        /// <param name="variables"></param>
        public override void FillVariables(InterpretationContext context, List<Variables.IVariable> variables)
        {
            Left.FillVariables(context, variables);
            Right.FillVariables(context, variables);
        }

        /// <summary>
        /// Indicates that the expression is an equality of the form variable == literal
        /// </summary>
        /// <returns></returns>
        public bool IsSimpleEquality(InterpretationContext context)
        {
            bool retVal = false;

            if (Operation == OPERATOR.EQUAL)
            {
                Variables.IVariable variable = Left.GetVariable(context);
                UnaryExpression value = Right as Interpreter.UnaryExpression;
                if (variable != null && value != null && value.Term != null)
                {
                    retVal = value.Term.LiteralValue != null;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Provides the string representation of the binary expression
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string retVal = "";

            retVal = Left.ToString();
            retVal += " " + Image(Operation) + " ";
            retVal += Right.ToString();

            return retVal;
        }

        /// <summary>
        /// Fills the list of literals with the literals found in this expression and sub expressions
        /// </summary>
        /// <param name="retVal"></param>
        public override void fillLiterals(List<Values.IValue> retVal)
        {
            Left.fillLiterals(retVal);
            Right.fillLiterals(retVal);
        }

        /// <summary>
        /// Provides the image of a given operator
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static string Image(OPERATOR op)
        {
            string retVal = null;

            for (int i = 0; i < Operators.Length; i++)
            {
                if (op == Operators[i])
                {
                    retVal = OperatorsImages[i];
                    break;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Provides the image of a given operator
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static string[] Images(OPERATOR[] ops)
        {
            string[] retVal = new string[ops.Length];

            for (int i = 0; i < ops.Length; i++)
            {
                retVal[i] = Image(ops[i]);
            }

            return retVal;
        }

        /// <summary>
        /// Provides the operator, based on its image
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static OPERATOR FindOperatorByName(string op)
        {
            OPERATOR retVal = OPERATOR.UNDEF;

            for (int i = 0; i < Operators.Length; i++)
            {
                if (OperatorsImages[i].CompareTo(op) == 0)
                {
                    retVal = Operators[i];
                    break;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Checks the expression and appends errors to the root tree node when inconsistencies are found
        /// </summary>
        /// <param name="context">The interpretation context</param>
        public override void checkExpression()
        {
            base.checkExpression();

            Left.checkExpression();
            Right.checkExpression();
        }

        /// <summary>
        /// Provides the graph of this function if it has been statically defined
        /// </summary>
        /// <param name="context">the context used to create the graph</param>
        /// <returns></returns>
        public override Functions.Graph createGraph(Interpreter.InterpretationContext context)
        {
            return Functions.Graph.createGraph(GetValue(context));
        }

        /// <summary>
        /// Creates the graph associated to this expression, when the given parameter ranges over the X axis
        /// </summary>
        /// <param name="context">The interpretation context</param>
        /// <param name="parameter">The parameters of *the enclosing function* for which the graph should be created</param>
        /// <returns></returns>
        public override Functions.Graph createGraphForParameter(InterpretationContext context, Parameter parameter)
        {
            Functions.Graph retVal = null;

            Functions.Graph leftGraph = Left.createGraphForParameter(context, parameter);
            Functions.Graph rightGraph = Right.createGraphForParameter(context, parameter);

            switch (Operation)
            {
                case Interpreter.BinaryExpression.OPERATOR.ADD:
                    retVal = leftGraph.AddGraph(rightGraph);
                    break;
                case Interpreter.BinaryExpression.OPERATOR.SUB:
                    retVal = leftGraph.SubstractGraph(rightGraph);
                    break;
                case Interpreter.BinaryExpression.OPERATOR.MULT:
                    retVal = leftGraph.MultGraph(rightGraph);
                    break;
                case Interpreter.BinaryExpression.OPERATOR.DIV:
                    retVal = leftGraph.DivGraph(rightGraph);
                    break;
            }

            return retVal;
        }

        /// <summary>
        /// Provides the graph associated to the function
        /// </summary>
        /// <param name="context"></param>
        /// <param name="function"></param>
        /// <param name="graph"></param>
        /// <param name="surface"></param>
        private Functions.Graph GetGraph(Interpreter.InterpretationContext context, Functions.Function function)
        {
            Functions.Graph retVal = null;

            if (function != null)
            {
                retVal = function.createGraph(context);
                if (retVal == null)
                {
                    AddError("Cannot apply operator on left function which cannot be interpreted as a Graph");
                }
            }

            return retVal;
        }

        /// <summary>
        /// Provides the surface of this function if it has been statically defined
        /// </summary>
        /// <param name="context">the context used to create the surface</param>
        /// <param name="xParam">The X axis of this surface</param>
        /// <param name="yParam">The Y axis of this surface</param>
        /// <returns>The surface which corresponds to this expression</returns>
        public override Functions.Surface createSurface(Interpreter.InterpretationContext context, Parameter xParam, Parameter yParam)
        {
            Functions.Surface retVal = null;

            Functions.Surface leftSurface = Left.createSurface(context, xParam, yParam);
            Functions.Surface rightSurface = Right.createSurface(context, xParam, yParam);

            switch (Operation)
            {
                case Interpreter.BinaryExpression.OPERATOR.ADD:
                    retVal = leftSurface.AddSurface(rightSurface);
                    break;
                case Interpreter.BinaryExpression.OPERATOR.SUB:
                    retVal = leftSurface.SubstractSurface(rightSurface);
                    break;
                case Interpreter.BinaryExpression.OPERATOR.MULT:
                    retVal = leftSurface.MultiplySurface(rightSurface);
                    break;
                case Interpreter.BinaryExpression.OPERATOR.DIV:
                    retVal = leftSurface.DivideSurface(rightSurface);
                    break;
            }

            retVal.XParameter = xParam;
            retVal.YParameter = yParam;

            return retVal;
        }

        /// <summary>
        /// Inverses the operator provided
        /// </summary>
        /// <param name="Operator"></param>
        /// <returns></returns>
        public static OPERATOR Inverse(OPERATOR Operator)
        {
            OPERATOR retVal = Operator;

            switch (Operator)
            {
                case OPERATOR.GREATER:
                    retVal = OPERATOR.LESS_OR_EQUAL;
                    break;

                case OPERATOR.GREATER_OR_EQUAL:
                    retVal = OPERATOR.LESS;
                    break;

                case OPERATOR.LESS:
                    retVal = OPERATOR.GREATER_OR_EQUAL;
                    break;

                case OPERATOR.LESS_OR_EQUAL:
                    retVal = OPERATOR.GREATER;
                    break;

                default:
                    throw new Exception("Cannot inverse operator " + Operator);
            }

            return retVal;
        }
    }
}