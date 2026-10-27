// Adapted from DiffPlex DiffPlex-1.7.0, DiffPlex/Differ.cs (Apache-2.0).
// Original project: https://github.com/mmanela/diffplex/tree/DiffPlex-1.7.0
// Modified for Bing.Utils: internal line-only kernel, explicit work stack,
// shared linear diagonals, cooperative cancellation and time-budget checks.
// See ThirdParty/DiffPlex/LICENSE.txt and SOURCE.md for attribution and changes.

namespace Bing.Text.Diff.Internal.DiffPlex;

/// <summary>
/// 来自 DiffPlex 1.7.0 的行级最短编辑路径内核，使用线性工作数组。
/// </summary>
internal static class LineDiffer
{
    /// <summary>
    /// 使用显式栈处理子问题，标记旧、新序列中发生变化的行。
    /// </summary>
    /// <param name="oldIds">旧行标识。</param>
    /// <param name="newIds">新行标识。</param>
    /// <param name="oldModified">旧行变化标记。</param>
    /// <param name="newModified">新行变化标记。</param>
    /// <param name="budget">计算预算。</param>
    internal static void MarkChanges(int[] oldIds, int[] newIds, bool[] oldModified, bool[] newModified, DiffBudget budget)
    {
        budget.Check();
        var size = checked(oldIds.Length + newIds.Length + 2);
        var forward = new int[size];
        var reverse = new int[size];
        var work = new Stack<(int StartA, int EndA, int StartB, int EndB)>();
        work.Push((0, oldIds.Length, 0, newIds.Length));
        while (work.Count != 0)
        {
            budget.Check();
            var (startA, endA, startB, endB) = work.Pop();
            while (startA < endA && startB < endB && oldIds[startA] == newIds[startB])
            {
                budget.Step();
                startA++;
                startB++;
            }
            while (startA < endA && startB < endB && oldIds[endA - 1] == newIds[endB - 1])
            {
                budget.Step();
                endA--;
                endB--;
            }
            if (startA < endA && startB < endB)
            {
                var res = CalculateEditLength(oldIds, startA, endA, newIds, startB, endB, forward, reverse, budget);
                if (res.EditLength <= 0) continue;
                if (res.LastEdit == Edit.DeleteRight && res.StartX - 1 > startA)
                    oldModified[--res.StartX] = true;
                else if (res.LastEdit == Edit.InsertDown && res.StartY - 1 > startB)
                    newModified[--res.StartY] = true;
                else if (res.LastEdit == Edit.DeleteLeft && res.EndX < endA)
                    oldModified[res.EndX++] = true;
                else if (res.LastEdit == Edit.InsertUp && res.EndY < endB)
                    newModified[res.EndY++] = true;
                work.Push((res.EndX, endA, res.EndY, endB));
                work.Push((startA, res.StartX, startB, res.StartY));
            }
            else
            {
                for (var i = startA; i < endA; i++) { budget.Step(); oldModified[i] = true; }
                for (var i = startB; i < endB; i++) { budget.Step(); newModified[i] = true; }
            }
        }
        budget.Check();
    }

    /// <summary>
    /// 双向对角线搜索中间匹配段；保留上游的编辑路径选择规则。
    /// </summary>
    private static EditLengthResult CalculateEditLength(int[] A, int startA, int endA, int[] B, int startB, int endB, int[] forwardDiagonal, int[] reverseDiagonal, DiffBudget budget)
    {
        if (null == A) throw new ArgumentNullException(nameof(A));
        if (null == B) throw new ArgumentNullException(nameof(B));

        if (A.Length == 0 && B.Length == 0)
        {
            return new EditLengthResult();
        }

        int N = endA - startA;
        int M = endB - startB;
        int MAX = M + N + 1;
        int HALF = MAX / 2;
        int delta = N - M;
        bool deltaEven = delta % 2 == 0;
        forwardDiagonal[1 + HALF] = 0;
        reverseDiagonal[1 + HALF] = N + 1;


        for (int D = 0; D <= HALF; D++)
        {
            budget.Step();
            // forward D-path
            Edit lastEdit;
            for (int k = -D; k <= D; k += 2)
            {
                budget.Step();
                int kIndex = k + HALF;
                int x, y;
                if (k == -D || (k != D && forwardDiagonal[kIndex - 1] < forwardDiagonal[kIndex + 1]))
                {
                    x = forwardDiagonal[kIndex + 1]; // y up    move down from previous diagonal
                    lastEdit = Edit.InsertDown;
                }
                else
                {
                    x = forwardDiagonal[kIndex - 1] + 1; // x up     move right from previous diagonal
                    lastEdit = Edit.DeleteRight;
                }
                y = x - k;
                int startX = x;
                int startY = y;
                while (x < N && y < M && A[x + startA] == B[y + startB])
                {
                    budget.Step();
                    x += 1;
                    y += 1;
                }

                forwardDiagonal[kIndex] = x;

                if (!deltaEven && k - delta >= -D + 1 && k - delta <= D - 1)
                {
                    int revKIndex = (k - delta) + HALF;
                    int revX = reverseDiagonal[revKIndex];
                    int revY = revX - k;
                    if (revX <= x && revY <= y)
                    {
                        return new EditLengthResult
                        {
                            EditLength = 2*D - 1,
                            StartX = startX + startA,
                            StartY = startY + startB,
                            EndX = x + startA,
                            EndY = y + startB,
                            LastEdit = lastEdit
                        };
                    }
                }
            }

            // reverse D-path
            for (int k = -D; k <= D; k += 2)
            {
                budget.Step();
                int kIndex = k + HALF;
                int x, y;
                if (k == -D || (k != D && reverseDiagonal[kIndex + 1] <= reverseDiagonal[kIndex - 1]))
                {
                    x = reverseDiagonal[kIndex + 1] - 1; // move left from k+1 diagonal
                    lastEdit = Edit.DeleteLeft;
                }
                else
                {
                    x = reverseDiagonal[kIndex - 1]; //move up from k-1 diagonal
                    lastEdit = Edit.InsertUp;
                }
                y = x - (k + delta);

                int endX = x;
                int endY = y;

                while (x > 0 && y > 0 && A[startA + x - 1] == B[startB + y - 1])
                {
                    budget.Step();
                    x -= 1;
                    y -= 1;
                }

                reverseDiagonal[kIndex] = x;

                if (deltaEven && k + delta >= -D && k + delta <= D)
                {
                    int forKIndex = (k + delta) + HALF;
                    int forX = forwardDiagonal[forKIndex];
                    int forY = forX - (k + delta);
                    if (forX >= x && forY >= y)
                    {
                        return new EditLengthResult
                        {
                            EditLength = 2*D,
                            StartX = x + startA,
                            StartY = y + startB,
                            EndX = endX + startA,
                            EndY = endY + startB,
                            LastEdit = lastEdit
                        };
                    }
                }
            }
        }

        throw new InvalidOperationException("差异算法未找到有效编辑路径。");
    }

    // Minimal upstream model; fields are private implementation details, never public API.
    private struct EditLengthResult
    {
        internal int EditLength;
        internal int StartX;
        internal int StartY;
        internal int EndX;
        internal int EndY;
        internal Edit LastEdit;
    }

    private enum Edit { DeleteRight, InsertDown, DeleteLeft, InsertUp }
}
