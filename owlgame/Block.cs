using System;
using System.Collections.Generic;
using System.Drawing;

namespace owlgame
{
    /// <summary>
    /// One tetromino: a <see cref="MatrixSize"/> x <see cref="MatrixSize"/> bit
    /// matrix, its colour and the board position of the matrix origin.
    ///
    /// A shape always gets the same colour, so the colour is derived from the
    /// shape index instead of a separate roll.
    /// </summary>
    public class Block
    {
        /// <summary>Side length of the square bit matrix.</summary>
        public const int MatrixSize = 5;

        /// <summary>How many shapes the matrix table holds.</summary>
        private const int ShapeCount = 7;

        // One shared generator for the whole game, guarded by a lock because
        // Random itself is not thread safe. A per-instance "new Random()" seeds
        // from Environment.TickCount (about 15 ms resolution), so two blocks
        // created back to back got identical shapes and colours - the "next"
        // preview used to show a copy of the current block on every new game.
        private static readonly Random random = new Random();
        private static readonly object randomLock = new object();

        /// <summary>Colour per shape index (classic Tetris mapping).</summary>
        private static readonly Color[] shapeColors =
        {
            Color.Cyan, Color.Magenta, Color.Lime, Color.Orange,
            Color.Blue, Color.Yellow, Color.Red
        };

        /// <summary>The seven shapes, each padded to a 5x5 matrix.</summary>
        private static readonly int[, ,] shapes =
        {
            // I
            {
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 },
                { 1, 1, 1, 1, 0 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 }
            },
            // T
            {
                { 0, 0, 0, 0, 0 },
                { 0, 0, 1, 0, 0 },
                { 0, 1, 1, 1, 0 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 }
            },
            // Z
            {
                { 0, 0, 0, 0, 0 },
                { 0, 1, 1, 0, 0 },
                { 0, 0, 1, 1, 0 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 }
            },
            // S
            {
                { 0, 0, 0, 0, 0 },
                { 0, 0, 1, 1, 0 },
                { 0, 1, 1, 0, 0 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 }
            },
            // J
            {
                { 0, 0, 0, 0, 0 },
                { 0, 1, 1, 0, 0 },
                { 0, 0, 1, 0, 0 },
                { 0, 0, 1, 0, 0 },
                { 0, 0, 0, 0, 0 }
            },
            // L
            {
                { 0, 0, 0, 0, 0 },
                { 0, 0, 1, 1, 0 },
                { 0, 0, 1, 0, 0 },
                { 0, 0, 1, 0, 0 },
                { 0, 0, 0, 0, 0 }
            },
            // O
            {
                { 0, 0, 0, 0, 0 },
                { 0, 0, 1, 1, 0 },
                { 0, 0, 1, 1, 0 },
                { 0, 0, 0, 0, 0 },
                { 0, 0, 0, 0, 0 }
            }
        };

        /// <summary>Filled cells of this block; 1 means filled.</summary>
        public readonly int[,] Matrix = new int[MatrixSize, MatrixSize];

        /// <summary>Colour used when this block is drawn.</summary>
        public Color BlockColor;

        /// <summary>Column of the matrix origin on the board.</summary>
        public int Column;

        /// <summary>Row of the matrix origin on the board.</summary>
        public int Row;

        /// <summary>Create a random block with its matrix origin at (column, row).</summary>
        public Block(int column, int row)
        {
            Column = column;
            Row = row;

            int shape = NextShapeIndex();
            BlockColor = shapeColors[shape];
            for (int i = 0; i < MatrixSize; ++i)
                for (int j = 0; j < MatrixSize; ++j)
                    Matrix[i, j] = shapes[shape, i, j];
        }

        /// <summary>
        /// Copy constructor: duplicates shape / colour / position without
        /// touching the random source. Rotation used to create a throw-away
        /// block just to hold a rotated matrix, which silently consumed random
        /// values on every key press.
        /// </summary>
        public Block(Block source)
        {
            Column = source.Column;
            Row = source.Row;
            BlockColor = source.BlockColor;
            for (int i = 0; i < MatrixSize; ++i)
                for (int j = 0; j < MatrixSize; ++j)
                    Matrix[i, j] = source.Matrix[i, j];
        }

        /// <summary>
        /// A rotated copy of this block, same position and colour. The caller
        /// decides whether the rotated cells actually fit on the board.
        /// </summary>
        public Block Rotated()
        {
            Block rotated = new Block(this);
            for (int i = 0; i < MatrixSize; ++i)
                for (int j = MatrixSize - 1; j >= 0; --j)
                    rotated.Matrix[j, i] = Matrix[i, MatrixSize - 1 - j];
            return rotated;
        }

        private static int NextShapeIndex()
        {
            lock (randomLock)
                return random.Next(ShapeCount);
        }
    }
}
