'(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

'This program is free software; you can redistribute it and/or modify
'it under the terms of the GNU General Public License as published by
'the Free Software Foundation; either version 2 of the License, or
'(at your option) any later version.

'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.

'You should have received a copy of the GNU General Public License
'along with this program; if not, write to the Free Software
'Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

' This is a plugin to create the category tree for a new WikiProject assessments
' department. It was thrown together as quickly as possible and doesn't represent
' the usual coding standard of the author(s)!

Namespace AutoWikiBrowser.Plugins.SDKSoftware.WPAssessmentsCatCreator
    Public NotInheritable Class WPAssessmentsCatCreator
        Implements IAWBPlugin

        ' AWB objects:
        Private Shared AWBForm As IAutoWikiBrowser

        ' Menu item:
        Private Const conOurName As String = "WPAssessmentsCatCreator"
        Private WithEvents OurMenuItem As New ToolStripMenuItem(conOurName & "Plugin")

        ' User input and state:
        Private WikiProjectName As String, TemplateName As String, ArticleType As String, ParentCat As String
        Private WP1 As String, WeAreRunning As Boolean

        ' Regex:
        Private CatRegex As Regex

        ' Enum:
        Private Enum Mode As Byte
            Classif
            Importance
            Priority
            Comments
            ArticlesByQ
            ArticlesByI
            ArticlesByP
        End Enum

        Public Sub Initialise(ByVal MainForm As IAutoWikiBrowser) Implements IAWBPlugin.Initialise
            ' Store object references:
            AWBForm = MainForm

            ' Add our menu item:
            With OurMenuItem
                .CheckOnClick = False
                .Checked = False
                .ToolTipText = "Start the " & conOurName & " plugin"
            End With

            MainForm.PluginsToolStripMenuItem.DropDownItems.Add(OurMenuItem)
        End Sub
        Public ReadOnly Property Name() As String Implements IAWBPlugin.Name
            Get
                Return conOurName
            End Get
        End Property
        Public Function ProcessArticle(ByVal sender As IAutoWikiBrowser, _
        ByVal ProcessArticleEventArgs As ProcessArticleEventArgs) As String _
        Implements IAWBPlugin.ProcessArticle

            If WeAreRunning Then
                If sender.ListMaker.Count <= 1 Then WeAreRunning = False

                With CatRegex.Match(ProcessArticleEventArgs.ArticleTitle)
                    If .Groups("class").Success Then
                        ProcessArticle = CategoryText(Mode.Classif, .Groups("class").Captures(0).ToString)
                    ElseIf .Groups("importance").Success Then
                        If .Groups("imppri").Captures(0).ToString = "importance" Then
                            ProcessArticle = CategoryText(Mode.Importance, .Groups("importance").Captures(0).ToString)
                        Else
                            ProcessArticle = CategoryText(Mode.Priority, .Groups("importance").Captures(0).ToString)
                        End If
                    ElseIf .Groups("art").Success Then
                        ProcessArticle = CategoryText(Mode.ArticlesByQ, .Groups("art").Captures(0).ToString)
                    ElseIf .Groups("byimp").Success Then
                        ProcessArticle = CategoryText(Mode.ArticlesByI, .Groups("byimp").Captures(0).ToString)
                    ElseIf .Groups("bypri").Success Then
                        ProcessArticle = CategoryText(Mode.ArticlesByP, .Groups("bypri").Captures(0).ToString)
                    ElseIf .Groups("comments").Success Then
                        ProcessArticle = CategoryText(Mode.Comments, .Groups("comments").Captures(0).ToString)
                    Else
                        Throw New ApplicationException("Unexpected page title. Probable fault in plugin")
                    End If
                End With

                ProcessArticleEventArgs.EditSummary = _
                   "Configuring WikiProject assessments category with alpha [[User:Kingboyk/CP|plugin]]"
            Else
                ProcessArticle = ProcessArticleEventArgs.ArticleText
            End If
        End Function

        ' Private routines:
        Private Function CategoryText(ByVal Mode As Mode, ByVal Argument As String) As String
            CategoryText = "This category contains articles that are supported by '''[[" & WikiProjectName & _
               "]]'''. Articles are automatically added to this category based on parameters in the {{tl|" & _
               TemplateName & "}} template." & vbCrLf & vbCrLf & WP1 & vbCrLf & "{{CategoryTOC}}" & _
               vbCrLf & vbCrLf

            Select Case Mode
                Case WPAssessmentsCatCreator.Mode.Classif, WPAssessmentsCatCreator.Mode.Comments
                    CategoryText = CategoryText & "[[" & ArticlesByQuality & "]]" & vbCrLf

                    If Mode = WPAssessmentsCatCreator.Mode.Classif Then
                        CategoryText = CategoryText & "[[Category:" & Argument & "-Class articles]]"
                    End If

                Case WPAssessmentsCatCreator.Mode.Importance
                    CategoryText = CategoryText & "[[" & ArticlesByImportance & "]]" & vbCrLf & _
                       "[[Category:" & Argument & "-importance articles]]"

                Case WPAssessmentsCatCreator.Mode.Priority
                    CategoryText = CategoryText & "[[" & ArticlesByPriority & "]]" & vbCrLf & _
                       "[[Category:" & Argument & "-importance articles]]"

                Case Else
                    CategoryText = CategoryText & "[[Category:" & ParentCat & "|*]]" & vbCrLf & _
                       "[[Category:Wikipedia 1.0 assessments]]"
            End Select

        End Function
        Private Sub AddImportanceCats(ByVal Importance As Boolean)
            Dim ImpPri As String

            If Importance Then
                ImpPri = "importance"
                AWBForm.ListMaker.Add(ArticlesByImportance)
            Else
                ImpPri = "priority"
                AWBForm.ListMaker.Add(ArticlesByPriority)
            End If

            For Each str2 As String In New String() {"Top", "High", "Mid", "Low", "Unknown"}
                AWBForm.ListMaker.Add("Category:" & str2 & "-" & ImpPri & " " & ArticleType & " articles")
            Next
        End Sub
        Private ReadOnly Property ArticlesByQuality() As String
            Get
                Return "Category:" & ArticleType & " articles by quality"
            End Get
        End Property
        Private ReadOnly Property ArticlesByImportance() As String
            Get
                Return "Category:" & ArticleType & " articles by importance"
            End Get
        End Property
        Private ReadOnly Property ArticlesByPriority() As String
            Get
                Return "Category:" & ArticleType & " articles by priority"
            End Get
        End Property
        Private ReadOnly Property GetCapitalisedArticleType() As String
            Get
                Return ArticleType.Substring(0, 1).ToUpper & ArticleType.Substring(1)
            End Get
        End Property

        ' Event handlers:
        Private Sub OurMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) _
        Handles OurMenuItem.Click
            AWBForm.Stop(Me)
            AWBForm.StopButton.PerformClick()

            If AWBForm.ListMaker.Count > 0 Then
                If MessageBox.Show( _
                "The article list is not empty. To empty it and proceed, hit OK. Otherwise, hit Cancel", _
                "Article list not empty", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation, _
                MessageBoxDefaultButton.Button2) = DialogResult.OK Then
                    WeAreRunning = True
                    AWBForm.ListMaker.Clear()
                    AWBForm.EditSummary.Text = ""
                Else
                    Exit Sub
                End If
            End If

            AWBForm.SkipNonExistentPagesCheckBox.Checked = False

            If MessageBox.Show("This is a simple script - presented as a plugin - for creating article " & _
               "assessment categories. It should not be run with automated edits switched on, and all " & _
               "edits should be reviewed. Other plugins should be switched off.", "Let's start", _
               MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1) _
               = DialogResult.Cancel Then GoTo ExitMeEarly

            WikiProjectName = InputBox("Enter the name of the WikiProject page, for example " & _
               "Wikipedia:WikiProject The Beatles" & vbCrLf & vbCrLf & _
               "You may use a piped name if you wish", "WikiProject Name", "Wikipedia:WikiProject").Trim
            If WikiProjectName = "" Then GoTo ExitMeEarly

            TemplateName = Regex.Replace(InputBox( _
               "Please enter the name of your project's template (without the template: prefix)", "Template").Trim, _
               "^template:", "", RegexOptions.IgnoreCase)
            If TemplateName = "" Then GoTo ExitMeEarly

            ArticleType = InputBox("Enter the name of your article type so we can build the " & _
               "categories" & vbCrLf & vbCrLf & _
               "For example, to build categories like ""FA-Class biography articles"" enter ""biography""" & _
               "(without the quotes)", "Article subject type").Trim
            If ArticleType = "" Then GoTo ExitMeEarly

            WP1 = "{{WP1|" & GetCapitalisedArticleType & "}}"

            For Each str As String In New String() {"A", "B", "GA", "FA", "Start", "Stub", "Unassessed"}
                AWBForm.ListMaker.Add("Category:" & str & "-Class " & ArticleType & " articles")
            Next
            AWBForm.ListMaker.Add("Category:" & ArticleType & " articles with comments")
            AWBForm.ListMaker.Add(ArticlesByQuality)

            If MessageBox.Show("Does your WikiProject track priority or importance?", "Priority/importance", _
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Select Case (MessageBox.Show("Do you use importance=?" & vbCrLf & _
                vbCrLf & "Hit yes for importance, no for priority, cancel to exit", _
                "Importance/priority", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                    Case DialogResult.Yes
                        AddImportanceCats(True)
                    Case DialogResult.No
                        AddImportanceCats(False)
                    Case DialogResult.Cancel
                        GoTo ExitMeEarly
                End Select
            End If

            ParentCat = Regex.Replace(InputBox("What's the name of the parent category for the by quality/by " & _
               "priority categories?" & vbCrLf & vbCrLf & "Example: Category:Military work group articles").Trim, _
               "^category:", "", RegexOptions.IgnoreCase)

            If ParentCat = "" Then GoTo ExitMeEarly

            CatRegex = New Regex("^Category:((?<class>[A-Za-z]*)-Class .* articles|(?<comments>" & _
               Regex.Escape(ArticleType) & " articles with comments)" & _
               "|(?<importance>[a-zA-Z]*)-(?<imppri>priority|importance)" & _
               "|.*((?<art>by quality)|(?<byimp>by importance)|(?<bypri>by priority))$)", _
               RegexOptions.ExplicitCapture)

            AWBForm.Start(Me)
ExitMe:
            Exit Sub

ExitMeEarly:
            AWBForm.ListMaker.Clear()
            WeAreRunning = False
        End Sub

        Public Sub Nudge(ByRef Cancel As Boolean) Implements IAWBPlugin.Nudge
            Cancel = True
        End Sub

        ' Do nothing:
        Public Function SaveSettings() As Object() Implements IAWBPlugin.SaveSettings
            Return Nothing
        End Function
        Public Sub LoadSettings(ByVal Prefs() As Object) Implements IAWBPlugin.LoadSettings
        End Sub
        Public Sub Reset() Implements IAWBPlugin.Reset
        End Sub
        Public Sub Nudged(ByVal Nudges As Integer) Implements IAWBPlugin.Nudged
        End Sub
    End Class
End Namespace