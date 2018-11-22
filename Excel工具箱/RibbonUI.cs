﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Tools.Ribbon;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Excel;
using System.Windows.Forms;

namespace Excel工具箱
{
    public partial class Ribbon1
    {
        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {
            mergesheets_HeadRowNum.SelectedItemIndex = 1;
            mergesheets_contentRowNum.SelectedItemIndex = 1;
        }

        private void mergebooks_BeginMerge_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.Application.ScreenUpdating = false;
            Excel.Workbook destWorkbook, sourceWorkbook;
            int currentSheetIndex = 1;
            int MergeNum;
            //打开文件，否则取消操作
            object FileOpen = Globals.ThisAddIn.Application.GetOpenFilename(FileFilter: "Excel 97-2003 工作簿(*.xls),*xls,Microsoft Excel文件(*.xlsx),*.xlsx", MultiSelect: true, Title: "请选择需要合并的工作簿");
            if (FileOpen.GetType() == typeof(bool)) return;
            MergeNum = ((System.Collections.IList)FileOpen).Count;
            //若需新建工作簿，则新建并设置为目标簿，否则以当前为目标簿
            try
            {
                Globals.ThisAddIn.Application.ActiveSheet.GetType();
            }
            catch
            {
                mergebooks_RequireNewBook.Checked = true;
            }
            if (mergebooks_RequireNewBook.Checked == true) destWorkbook = Globals.ThisAddIn.Application.Workbooks.Add();
            else destWorkbook = Globals.ThisAddIn.Application.ActiveWorkbook;
            for (int counter = 1; counter <= MergeNum; counter++)
            {
                //打开文件单中文件并设为源
                sourceWorkbook = Globals.ThisAddIn.Application.Workbooks.Open
                    (Filename: (string)((System.Collections.IList)FileOpen)[counter]);
                //复制
                foreach (Excel.Worksheet sourceWorksheet in sourceWorkbook.Worksheets)
                {
                    if (mergebooks_MergeAllSheets.Checked == false && sourceWorksheet.Index > 1) break;
                    sourceWorksheet.Copy(destWorkbook.Worksheets[currentSheetIndex]);
                    currentSheetIndex++;
                }
                //关闭
                sourceWorkbook.Close();
            }
            //若新建了工作簿，删除默认的"Sheet1"
            if (mergebooks_RequireNewBook.Checked)
            {
                Globals.ThisAddIn.Application.DisplayAlerts = false;
                destWorkbook.Worksheets[currentSheetIndex].Delete();
                Globals.ThisAddIn.Application.DisplayAlerts = true;
            }
            Globals.ThisAddIn.Application.ScreenUpdating = true;
        }

        private void mergesheets_BeginMerge_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.Application.ScreenUpdating = false;
            Globals.ThisAddIn.Application.ActiveWorkbook.Sheets[1].Activate();
            Excel.Worksheet destWorksheet = Globals.ThisAddIn.Application.Worksheets.Add();
            Excel.Workbook sourceWorkbook = Globals.ThisAddIn.Application.ActiveWorkbook;
            try
            {
                destWorksheet.Name = "Merge";
            }
            catch
            {
                Random random = new Random();
                destWorksheet.Name = "Merge" + random.Next(1, 10000).ToString();
            }

            if (mergesheets_contentRowNum.SelectedItemIndex != 0)
            {
                int HeadRowNum, CopyRowNum, CopyRowBegin, CopyRowEnd, CurrentRowIndex;
                HeadRowNum = mergesheets_HeadRowNum.SelectedItemIndex;
                CopyRowNum = mergesheets_contentRowNum.SelectedItemIndex;
                CopyRowBegin = HeadRowNum + 1;
                CopyRowEnd = HeadRowNum + CopyRowNum;
                CurrentRowIndex = 1;
                if (mergesheets_HeadRowNum.SelectedItemIndex != 0)
                {
                    RowCP(sourceWorkbook.Sheets[2].Rows["1:" + mergesheets_HeadRowNum.SelectedItemIndex.ToString()], destWorksheet.Rows[1], mergesheets_isFunctionEmbeded.Checked);
                    CurrentRowIndex = CurrentRowIndex + mergesheets_HeadRowNum.SelectedItemIndex;
                }
                for (int CurrentSheetIndex = 2; CurrentSheetIndex <= Globals.ThisAddIn.Application.ActiveWorkbook.Worksheets.Count; CurrentSheetIndex++)
                {
                    RowCP(sourceWorkbook.Sheets[CurrentSheetIndex].Rows[CopyRowBegin.ToString() + ":" + CopyRowEnd.ToString()], destWorksheet.Rows[CurrentRowIndex], mergesheets_isFunctionEmbeded.Checked);
                    CurrentRowIndex = CurrentRowIndex + CopyRowNum;
                }
                destWorksheet.Cells[1].Select();
            }

            if (mergesheets_contentRowNum.SelectedItemIndex == 0)
            {
                int HeadRowNum, CopyRowBegin, CopyRowEnd, CurrentRowIndex;
                HeadRowNum = mergesheets_HeadRowNum.SelectedItemIndex;
                CopyRowBegin = HeadRowNum + 1;
                CurrentRowIndex = 1;
                if (mergesheets_HeadRowNum.SelectedItemIndex != 0)
                {
                    RowCP(sourceWorkbook.Sheets[2].Rows["1:" + mergesheets_HeadRowNum.SelectedItemIndex.ToString()], destWorksheet.Rows[1], mergesheets_isFunctionEmbeded.Checked);
                    CurrentRowIndex = CurrentRowIndex + mergesheets_HeadRowNum.SelectedItemIndex;
                }
                for (int CurrentSheetIndex = 2; CurrentSheetIndex <= Globals.ThisAddIn.Application.ActiveWorkbook.Worksheets.Count; CurrentSheetIndex++)
                {
                    CopyRowEnd = FirstEmptyRowOf(sourceWorkbook.Worksheets[CurrentSheetIndex], 10) - 1;
                    MessageBox.Show("SUMMARY:\nCurrentRowToWrite:" + CurrentRowIndex.ToString() + "\nCopying:Sheets[" + CurrentSheetIndex.ToString() + "].Rows[" + CopyRowBegin.ToString() + ":" + CopyRowEnd.ToString() + "]\nDest:" + CurrentRowIndex.ToString() + "\n" + (CurrentRowIndex + 1 + CopyRowEnd - CopyRowBegin).ToString() + "rows have been copied");
                    RowCP(sourceWorkbook.Sheets[CurrentSheetIndex].Rows[CopyRowBegin.ToString() + ":" + CopyRowEnd.ToString()], destWorksheet.Rows[CurrentRowIndex], mergesheets_isFunctionEmbeded.Checked);
                    CurrentRowIndex = CurrentRowIndex + 1 + CopyRowEnd - CopyRowBegin;
                }
                destWorksheet.Cells[1].Select();
            }

            Clipboard.Clear();
            Globals.ThisAddIn.Application.ScreenUpdating = true;
        }

        private void others_DeleteOtherSheets_Click(object sender, RibbonControlEventArgs e)
        {
            double RowsToReserve;
            try
            {
                RowsToReserve = Globals.ThisAddIn.Application.InputBox(Prompt: "保留几张表？默认1张！", Type: 1);
            }
            catch
            {
                return;
            }
            if (RowsToReserve < 1) RowsToReserve = 1;
            Globals.ThisAddIn.Application.DisplayAlerts = false;
            foreach (Excel.Worksheet worksheet in Globals.ThisAddIn.Application.ActiveWorkbook.Worksheets)
            {
                if (worksheet.Index <= RowsToReserve) continue;
                worksheet.Delete();
            }
            Globals.ThisAddIn.Application.DisplayAlerts = true;
        }

        private int FirstEmptyRowOf(Excel.Worksheet testSheet, int testCellsNumEachRow)
        {
            int counterR, counterC;
            bool isEmpty;
            for (counterR = 1; counterR < 10000; counterR++)
            {
                isEmpty = true;
                for (counterC = 1; counterC < testCellsNumEachRow; counterC++)
                {
                    if (testSheet.Cells[counterR, counterC].Text.Trim() != "")
                    {
                        isEmpty = false;
                        continue;
                    }
                }
                if (isEmpty) return counterR;
            }
            return 0;
        }

        private void RowCP(Excel.Range source, Excel.Range dest, bool functionEmbeded)
        {
            if (functionEmbeded)
            {
                source.Copy();
                dest.PasteSpecial(XlPasteType.xlPasteAllUsingSourceTheme, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);
                dest.PasteSpecial(XlPasteType.xlPasteValues, XlPasteSpecialOperation.xlPasteSpecialOperationNone, false, false);

            }
            else source.Copy(dest);
        }

        private void updateView_Click(object sender, RibbonControlEventArgs e)
        {
            if (updateView.Checked) Globals.ThisAddIn.Application.ScreenUpdating = true;
            else Globals.ThisAddIn.Application.ScreenUpdating = false;
        }

        private void showAlert_Click(object sender, RibbonControlEventArgs e)
        {
            if (showAlert.Checked) Globals.ThisAddIn.Application.DisplayAlerts = true;
            else Globals.ThisAddIn.Application.DisplayAlerts = false;
        }

        private void testBtn_Click(object sender, RibbonControlEventArgs e)
        {

        }

        private void others_ClrClipboard_Click(object sender, RibbonControlEventArgs e)
        {
            Clipboard.Clear();

        }

        private void others_LookForFirstEmptyRow_Click(object sender, RibbonControlEventArgs e)
        {
            Globals.ThisAddIn.Application.ActiveSheet.Rows[FirstEmptyRowOf(Globals.ThisAddIn.Application.ActiveSheet, 10)].Select();
        }

        private void help_About_Click(object sender, RibbonControlEventArgs e)
        {
            //todo:Draw a about box...
            //AboutBox aboutBox = new AboutBox();
            //aboutBox.Show();
        }
    }
}