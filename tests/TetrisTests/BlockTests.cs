using System;
using System.Collections.Generic;
using System.Drawing;
using owlgame;

namespace TetrisTests
{
    /// <summary>
    /// Shape table, randomisation and rotation. No UI involved.
    /// </summary>
    public class BlockTests
    {
        private const int SampleSize = 400;

        private static string ShapeOf(Block block)
        {
            string s = "";
            for (int i = 0; i < Block.MatrixSize; i++)
            {
                for (int j = 0; j < Block.MatrixSize; j++)
                    s += block.Matrix[i, j] == GameBoard.Settled ? "#" : ".";
                s += "|";
            }
            return s;
        }

        private static int FilledCells(Block block)
        {
            int n = 0;
            for (int i = 0; i < Block.MatrixSize; i++)
                for (int j = 0; j < Block.MatrixSize; j++)
                    if (block.Matrix[i, j] == GameBoard.Settled)
                        n++;
            return n;
        }

        [Test]
        public void ShapeTableHoldsExactlySevenShapes()
        {
            HashSet<string> shapes = new HashSet<string>();
            for (int k = 0; k < SampleSize; k++)
                shapes.Add(ShapeOf(new Block(8, 0)));

            Assert.AreEqual(7, shapes.Count, "the table should hold 7 tetrominoes");
        }

        [Test]
        public void EveryShapeUsesFourCells()
        {
            HashSet<string> shapes = new HashSet<string>();
            for (int k = 0; k < SampleSize; k++)
            {
                Block b = new Block(8, 0);
                shapes.Add(ShapeOf(b));
                Assert.AreEqual(4, FilledCells(b), "every tetromino is made of 4 cells, shape=" + ShapeOf(b));
            }
            Assert.IsTrue(shapes.Count == 7, "expected to see all 7 shapes, saw " + shapes.Count);
        }

        [Test]
        public void EveryCellIsEmptyOrSettled()
        {
            for (int k = 0; k < SampleSize; k++)
            {
                Block b = new Block(8, 0);
                for (int i = 0; i < Block.MatrixSize; i++)
                    for (int j = 0; j < Block.MatrixSize; j++)
                        Assert.IsTrue(b.Matrix[i, j] == GameBoard.Empty || b.Matrix[i, j] == GameBoard.Settled,
                                      "matrix cells are only Empty or Settled");
            }
        }

        [Test]
        public void EachShapeAlwaysGetsTheSameColour()
        {
            Dictionary<string, Color> seen = new Dictionary<string, Color>();
            for (int k = 0; k < SampleSize; k++)
            {
                Block b = new Block(8, 0);
                string shape = ShapeOf(b);
                if (seen.ContainsKey(shape))
                    Assert.AreEqual(seen[shape], b.BlockColor, "shape kept its colour");
                else
                    seen[shape] = b.BlockColor;
            }
        }

        [Test]
        public void SevenDistinctColours()
        {
            HashSet<Color> colours = new HashSet<Color>();
            for (int k = 0; k < SampleSize; k++)
                colours.Add(new Block(8, 0).BlockColor);

            Assert.AreEqual(7, colours.Count, "one colour per shape");
        }

        [Test]
        public void BackToBackBlocksAreNotIdentical()
        {
            // Regression for the old "new Random() per instance" bug: two blocks
            // created back to back used to be identical every single time.
            int identicalPairs = 0;
            int pairs = 25;
            for (int k = 0; k < pairs; k++)
            {
                Block a = new Block(8, 0);
                Block b = new Block(8, 0);
                if (ShapeOf(a) == ShapeOf(b) && a.BlockColor == b.BlockColor)
                    identicalPairs++;
            }

            // Two draws match with probability 1/7, so a couple of collisions is
            // normal. All 25 matching is the bug.
            Assert.IsTrue(identicalPairs <= 10,
                "back-to-back blocks should differ; " + identicalPairs + "/" + pairs + " were identical");
        }

        [Test]
        public void RotatingFourTimesRestoresTheShape()
        {
            for (int k = 0; k < 60; k++)
            {
                Block original = new Block(8, 0);
                string before = ShapeOf(original);

                Block rotated = original;
                for (int turn = 0; turn < 4; turn++)
                    rotated = rotated.Rotated();

                Assert.AreEqual(before, ShapeOf(rotated), "4 rotations return to the start");
            }
        }

        [Test]
        public void RotationKeepsPositionAndColour()
        {
            for (int k = 0; k < 60; k++)
            {
                Block original = new Block(6, 4);
                Block rotated = original.Rotated();

                Assert.AreEqual(original.Column, rotated.Column, "column unchanged");
                Assert.AreEqual(original.Row, rotated.Row, "row unchanged");
                Assert.AreEqual(original.BlockColor, rotated.BlockColor, "colour unchanged");
                Assert.AreEqual(4, FilledCells(rotated), "rotation keeps 4 cells");
            }
        }

        [Test]
        public void RotationDoesNotConsumeTheRandomSequence()
        {
            // Rotation builds a copy; if it went through the random generator the
            // sequence would shift and this loop would see fewer distinct shapes.
            Block b = new Block(8, 0);
            for (int k = 0; k < 50; k++)
                b = b.Rotated();

            HashSet<string> shapes = new HashSet<string>();
            for (int k = 0; k < SampleSize; k++)
                shapes.Add(ShapeOf(new Block(8, 0)));

            Assert.AreEqual(7, shapes.Count, "all 7 shapes still reachable after many rotations");
        }

        [Test]
        public void CopyConstructorIsIndependent()
        {
            Block original = new Block(8, 0);
            Block copy = new Block(original);

            int originalFilled = FilledCells(original);
            for (int i = 0; i < Block.MatrixSize; i++)
                for (int j = 0; j < Block.MatrixSize; j++)
                    copy.Matrix[i, j] = GameBoard.Empty;

            Assert.AreEqual(originalFilled, FilledCells(original),
                "clearing the copy must not touch the original");
            Assert.AreEqual(0, FilledCells(copy), "the copy itself was cleared");
        }

        [Test]
        public void ConstructorStoresItsPosition()
        {
            Block b = new Block(7, 3);
            Assert.AreEqual(7, b.Column, "column stored");
            Assert.AreEqual(3, b.Row, "row stored");
        }
    }
}
