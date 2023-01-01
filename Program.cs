using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
    class Operator
    {
        public string Symbol { get; set; }
        public int Precedence { get; set; }
        public bool RightAssociativity { get; set; }

        public Operator(string symbol, int precedence, bool rightAssociativity)
        {
            Symbol = symbol;
            Precedence = precedence;
            RightAssociativity = rightAssociativity;
        }

        public override string ToString()
        {
            return String.Format("Operator \"{0}\" | Precedence {1} | Associativity {2}",
                Symbol, Precedence, RightAssociativity ? "Right" : "Left");
        }
    }

    class OperatorContainer
    {
        List<Operator> operators;

        public OperatorContainer(List<Operator> operators)
        {
            this.operators = operators;
        }

        public Operator FindOperator(string symbol)
        {
            foreach (Operator _operator in this.operators)
            {
                if (_operator.Symbol == symbol)
                {
                    return _operator;
                }
            }

            throw new ArgumentException(String.Format("An operator \"{0}\" is not defined", symbol));
        }

        public bool IsOperator(string symbol)
        {
            foreach (Operator _operator in this.operators)
            {
                if (_operator.Symbol == symbol)
                {
                    return true;
                }
            }

            return false;
        }

        public double ApplyOperator(double firstNumber, double secondNumber, string operatorSymbol)
        {
            switch (operatorSymbol)
            {
                case "*":
                    return firstNumber * secondNumber;

                case "/":
                    return firstNumber / secondNumber;

                case "-":
                    return firstNumber - secondNumber;

                case "+":
                    return firstNumber + secondNumber;

                default:
                    throw new ApplicationException(String.Format("Operation \"{0}\" is not recognised", operatorSymbol));
            }
        }
    }

    class Expression
    {
        public string Notation { get; set; }
        private OperatorContainer AvailableOperators;

        public Expression(string notation, OperatorContainer availableOperators)
        {
            this.Notation = notation;
            this.AvailableOperators = availableOperators;

            RemoveWhiteSpaces();

            // Check whether parentheses are used correctly
            if (!CheckParenthesesMatching())
            {
                throw new ArgumentException("Unmatched parentheses found");
            }

            // Fix loose unsigned numbers. E.g. -2*8 => (0-2)*8
            while (!NoUnsignedNumbers())
            {
                FixUnsignedNumbersOnce();
            }
        }

        public double Calculate()
        {
            int index = 0;
            List<string> parts = new List<string>();

            while (index < this.Notation.Length)
            {
                string segment = ReadSegment(index);
                parts.Add(segment);
                index += segment.Length;
            }

            try
            {
                parts = ToReversePolishNotation(parts);
            }
            catch (Exception e)
            {
                throw new Exception("An error occured while parsing: ", e);
            }


            double result = CalculateRPN(parts);

            return result;
        }

        private void RemoveWhiteSpaces()
        {
            StringBuilder newNotation = new StringBuilder();

            foreach (char symbol in this.Notation)
            {
                if (symbol != ' ')
                {
                    newNotation.Append(symbol);
                }
            }

            this.Notation = newNotation.ToString();
        }

        private void FixUnsignedNumbersOnce()
        {
            StringBuilder fixedNotation = new StringBuilder();

            for (int i = 0; i < this.Notation.Length; i++)
            {
                if (this.Notation[i] == '-' && (i == 0 || AvailableOperators.IsOperator(this.Notation[i - 1].ToString()) ||
                    this.Notation[i - 1] == ')' || this.Notation[i - 1] == '('))
                {
                    fixedNotation.Append("(0-");

                    string nextSegment = ReadSegment(i + 1);

                    if (double.TryParse(nextSegment, out double result))
                    {
                        fixedNotation.Append(nextSegment);
                        i += nextSegment.Length;
                    }
                    else if (nextSegment == "(")
                    {
                        do
                        {
                            fixedNotation.Append(nextSegment);
                            i += nextSegment.Length;
                            nextSegment = ReadSegment(i + 1);
                        } while (nextSegment != ")");
                    }

                    fixedNotation.Append(")");
                }
                else
                {
                    fixedNotation.Append(this.Notation[i]);
                }
            }

            this.Notation = fixedNotation.ToString();
        }

        private string ReadSegment(int startLocation)
        {
            StringBuilder segment = new StringBuilder();
            int index = startLocation;

            while (index < this.Notation.Length)
            {
                string symbol = this.Notation[index].ToString();

                segment.Append(symbol);

                if (AvailableOperators.IsOperator(symbol) || symbol == "(" || symbol == ")")
                {
                    break;
                }

                if (index + 1 < this.Notation.Length && (char.IsDigit(this.Notation[index + 1]) || this.Notation[index + 1] == '.'))
                {
                    index++;
                }
                else
                {
                    break;
                }
            }

            return segment.ToString();
        }

        private bool NoUnsignedNumbers()
        {
            for (int i = 0; i < this.Notation.Length; i++)
            {
                if (this.Notation[i] == '-' && (i == 0 || AvailableOperators.IsOperator(this.Notation[i - 1].ToString()) ||
                    this.Notation[i - 1] == ')' || this.Notation[i - 1] == '('))
                {
                    return false;
                }
            }

            return true;
        }

        private List<string> ToReversePolishNotation(List<string> parts)
        {
            List<string> output = new List<string>();
            Stack<Operator> operatorStack = new Stack<Operator>();

            foreach (string token in parts)
            {
                if (double.TryParse(token, out double result))
                {
                    output.Add(token);
                }
                else if (AvailableOperators.IsOperator(token))
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek().Symbol != "(" &&
                         (operatorStack.Peek().Precedence > AvailableOperators.FindOperator(token).Precedence ||
                         (operatorStack.Peek().Precedence == AvailableOperators.FindOperator(token).Precedence &&
                         !AvailableOperators.FindOperator(token).RightAssociativity)))
                    {
                        Operator o2 = operatorStack.Pop();
                        output.Add(o2.Symbol);
                    }

                    operatorStack.Push(AvailableOperators.FindOperator(token));
                }
                else if (token == "(")
                {
                    operatorStack.Push(new Operator("(", 0, true));
                }
                else if (token == ")")
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek().Symbol != "(")
                    {
                        output.Add(operatorStack.Pop().Symbol);
                    }

                    if (operatorStack.Peek().Symbol == "(")
                    {
                        operatorStack.Pop();
                    }
                }
                else
                {
                    throw new ArgumentException(String.Format("Unrecognised character \"{0}\". Make sure you have declared it as an operator " +
                        "before using it.", token));
                }
            }

            while (operatorStack.Count > 0)
            {
                output.Add(operatorStack.Pop().Symbol);
            }

            return output;
        }

        private double CalculateRPN(List<string> parts)
        {
            Stack<double> resultStack = new Stack<double>();

            foreach (string token in parts)
            {
                double result;

                if (double.TryParse(token, out result))
                {
                    resultStack.Push(result);
                }
                else
                {
                    if (resultStack.Count < 2)
                    {
                        throw new ArgumentException("Incorrect expression format, please check for typing errors");
                    }

                    double secondNumber = resultStack.Pop();
                    double firstNumber = resultStack.Pop();

                    resultStack.Push(AvailableOperators.ApplyOperator(firstNumber, secondNumber, token));
                }
            }

            if (resultStack.Count == 0)
            {
                throw new ArgumentException("Incorrect expression format, please check for typing errors");
            }

            return resultStack.Pop();
        }

        private bool CheckParenthesesMatching()
        {
            int openParenthesesCount = 0;

            foreach (char symbol in this.Notation)
            {
                if (symbol == '(')
                {
                    openParenthesesCount++;
                }
                else if (symbol == ')')
                {
                    openParenthesesCount--;
                }
            }

            if (openParenthesesCount == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            List<Operator> Operators = new List<Operator>()
            {
                new Operator("*", 3, false),
                new Operator("/", 3, false),
                new Operator("+", 2, false),
                new Operator("-", 2, false)
            };

            OperatorContainer AvailableOperators = new OperatorContainer(Operators);

            // Class used to test functionality and special cases
            TestExpressionClass(AvailableOperators);
        }

        private static void TestExpressionClass(OperatorContainer AvailableOperators)
        {
            // Test case 1: Normal
            Expression expression1 = new Expression("-2 * 4 + (5.5 - -7.25)", AvailableOperators);
            Console.WriteLine("Expression: \"{0}\" Status: {1}", "-2 * 4 + (5.5 - -7.25)", expression1.Calculate() == 4.75 ? "Passed" : "Failed");

            // Test case 2: Just a number
            Expression expression2 = new Expression("87.5", AvailableOperators);
            Console.WriteLine("Expression: \"{0}\" Status: {1}", "87.5", expression2.Calculate() == 87.5 ? "Passed" : "Failed");

            // Test case 3: Just a negative number
            Expression expression3 = new Expression("-87.5", AvailableOperators);
            Console.WriteLine("Expression: \"{0}\" Status: {1}", "-87.5", expression3.Calculate() == -87.5 ? "Passed" : "Failed");

            // Test case 4: Empty expression
            try
            {
                Expression expression4 = new Expression("", AvailableOperators);
                Console.WriteLine("Expression: \"{0}\" Status: {1}", "", expression4.Calculate() == 0 ? "Passed" : "Failed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Empty string test passed: exception caught");
            }

            // Test case 5: Just an operator
            try
            {
                Expression expression5 = new Expression(" * ", AvailableOperators);
                Console.WriteLine("Expression: \"{0}\" Status: {1}", "", expression5.Calculate() == 0 ? "Passed" : "Failed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Single operator test passed: exception caught");
            }

            // Test case 6: Excess operators
            try
            {
                Expression expression6 = new Expression("-2 * / 4 + (5.5 - -7.25)", AvailableOperators);
                Console.WriteLine("Expression: \"{0}\" Status: {1}", "", expression6.Calculate() == 0 ? "Passed" : "Failed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Excess operator test passed: exception caught");
            }

            // Test case 7: Unrecognised operators/symbols
            try
            {
                Expression expression7 = new Expression("22 * 14 % 2", AvailableOperators);
                Console.WriteLine("Expression: \"{0}\" Status: {1}", "", expression7.Calculate() == 0 ? "Passed" : "Failed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unrecognised operator test passed: exception caught");
            }

            // Test case 8: Unmatching parentheses
            try
            {
                Expression expression8 = new Expression("-7 * 14 + (22 * (8 - 12)", AvailableOperators);
                Console.WriteLine("Expression: \"{0}\" Status: {1}", "", expression8.Calculate() == 0 ? "Passed" : "Failed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unmatching parentheses test passed: exception caught");
            }

            // Test case 9: More complex test
            Expression expression9 = new Expression("-(-1) * 3 - -1", AvailableOperators);
            Console.WriteLine("Expression: \"{0}\" Status: {1}", "-(-1) * 3 - -1", expression9.Calculate() == 4 ? "Passed" : "Failed");
        }
    }
}
