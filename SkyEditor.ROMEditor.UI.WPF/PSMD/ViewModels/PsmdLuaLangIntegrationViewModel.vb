﻿Imports System.Collections.ObjectModel
Imports System.IO
Imports SkyEditor.CodeEditor
Imports SkyEditor.Core.IO
Imports SkyEditor.Core.UI
Imports SkyEditor.ROMEditor.MysteryDungeon.PSMD
Imports SkyEditor.ROMEditor.Windows.MysteryDungeon.PSMD
Imports SkyEditor.UI.WPF

Namespace PSMD.ViewModels
    Public Class PsmdLuaLangIntegrationViewModel
        Inherits GenericViewModel(Of LuaCodeFile)
        Implements INotifyModified

        Public Property MessageTabs As ObservableCollection(Of TabItem)

        Public Overrides Function SupportsObject(Obj As Object) As Boolean
            Return MyBase.SupportsObject(Obj) AndAlso CurrentPluginManager.CurrentIOUIManager.GetProjectOfOpenModel(Obj) IsNot Nothing
        End Function

        Public Overrides Async Sub SetModel(model As Object)
            MyBase.SetModel(model)

            Dim codeFile As LuaCodeFile = model
            Dim project = CurrentPluginManager.CurrentIOUIManager.GetProjectOfOpenModel(codeFile)

            Dim messageFiles As New Dictionary(Of String, MessageBin)
            For Each item In Directory.GetDirectories(Path.Combine(project.GetRootDirectory, "Languages"), "*", SearchOption.TopDirectoryOnly)
                Dim msgfile = New MessageBin
                Dim filename = Path.Combine(item, Path.GetFileNameWithoutExtension(codeFile.Filename))

                Dim exists As Boolean = False
                If File.Exists(filename) Then
                    exists = True
                ElseIf File.Exists(filename & ".bin") Then
                    filename &= ".bin"
                    exists = True
                End If

                If exists Then
                    Await msgfile.OpenFile(filename, CurrentPluginManager.CurrentIOProvider)
                    messageFiles.Add(Path.GetFileName(item), msgfile)
                End If
            Next

            MessageTabs = New ObservableCollection(Of TabItem)
            For Each item In messageFiles
                Dim m As New MessageBinViewModel
                m.SetPluginManager(CurrentPluginManager)
                m.SetModel(item.Value)
                AddHandler m.Modified, AddressOf Me.OnModified

                Dim p As New ObjectControlPlaceholder
                p.CurrentPluginManager = Me.CurrentPluginManager
                p.ObjectToEdit = m

                Dim t As New TabItem
                t.Header = item.Key
                t.Content = p
                MessageTabs.Add(t)
            Next
        End Sub

        Public Overrides Sub UpdateModel(model As Object)
            MyBase.UpdateModel(model)

            For Each item As TabItem In MessageTabs
                DirectCast(DirectCast(item.Content, ObjectControlPlaceholder).ObjectToEdit, MessageBinViewModel).Save(CurrentPluginManager.CurrentIOProvider)
            Next
        End Sub

        Public Event Modified As INotifyModified.ModifiedEventHandler Implements INotifyModified.Modified

        Private Sub OnModified(sender As Object, e As EventArgs)
            RaiseEvent Modified(Me, e)
        End Sub
    End Class
End Namespace

