﻿Imports System.Reflection
Imports DS_ROM_Patcher
Imports SkyEditor.Core
Imports SkyEditor.Core.Projects
Imports SkyEditor.ROMEditor.Projects

Public Class DSModSolution
    Inherits Solution

    Public Overrides Function CanCreateDirectory(Path As String) As Boolean
        Return True
    End Function

    Public Overrides Function CanCreateProject(Path As String) As Boolean
        Return (Path.Replace("\", "/").TrimStart("/") = "")
    End Function

    Public Overrides Function GetSupportedProjectTypes(Path As String, manager As PluginManager) As IEnumerable(Of TypeInfo)
        Dim baseRomProject As BaseRomProject = GetProjectsByName(Me.Settings("BaseRomProject")).FirstOrDefault
        If baseRomProject Is Nothing OrElse baseRomProject.RomSystem Is Nothing OrElse baseRomProject.GameCode Is Nothing Then
            Return {}
        Else
            Dim matches As New List(Of TypeInfo)
            For Each item In manager.GetRegisteredObjects(GetType(GenericModProject))
                Dim games = item.GetSupportedGameCodes
                Dim match As Boolean = False
                For Each t In games
                    Dim r As New Text.RegularExpressions.Regex(t)
                    If r.IsMatch(baseRomProject.GameCode) Then
                        matches.Add(item.GetType)
                    End If
                Next
            Next
            matches.Add(GetType(DSModPackProject))
            Return matches
        End If
    End Function

    Private Async Sub DSModSolution_ProjectAdded(sender As Object, e As ProjectAddedEventArgs) Handles Me.ProjectAdded
        If TypeOf e.Project Is GenericModProject Then
            Dim m = DirectCast(e.Project, GenericModProject)
            m.ProjectReferenceNames = New List(Of String)
            m.ProjectReferenceNames.Add(Me.Settings("BaseRomProject"))
            m.ModDependenciesBefore = New List(Of String)
            m.ModDependenciesAfter = New List(Of String)
            m.ModName = e.Project.Name
            m.ModVersion = "1.0.0"
            m.ModAuthor = "Unknown"
            m.ModDescription = "A generic Mod"
            m.Homepage = ""

            Await m.RunInitialize

            For Each item In Me.GetAllProjects
                If TypeOf item Is DSModPackProject Then
                    Dim modPack = DirectCast(item, DSModPackProject)

                    'If the mod we just added targets the same base ROM as this modpack...
                    If m.ProjectReferenceNames.Contains(modPack.BaseRomProject) Then
                        '...then we add this mod to the modpack.
                        If Not modPack.ProjectReferenceNames.Contains(m.Name) Then
                            modPack.ProjectReferenceNames.Add(m.Name)
                        End If

                    End If

                End If
            Next
        ElseIf TypeOf e.Project Is DSModPackProject Then
            Dim m = DirectCast(e.Project, DSModPackProject)
            m.Info = New ModpackInfo With {.Name = Me.Name}
            m.Info.Name = Me.Name
            m.Info.ShortName = Me.Name.Substring(0, Math.Min(Me.Name.Length, 10))
            m.Info.Author = "Unknown"
            m.Info.Version = "1.0.0"
            Dim baseRomProject As BaseRomProject = GetProjectsByName(Me.Settings("BaseRomProject")).FirstOrDefault
            If baseRomProject IsNot Nothing Then
                m.Info.System = baseRomProject.RomSystem
                m.Info.GameCode = baseRomProject.GameCode
                m.BaseRomProject = Me.Settings("BaseRomProject")
            End If
        End If
    End Sub

    Public Overrides Async Function Load() As Task
        Await MyBase.Load
        Dim setting = Settings("IsInitialLoadComplete")
        If Not (setting IsNot Nothing AndAlso (TypeOf setting Is Boolean AndAlso DirectCast(setting, Boolean) = True)) Then
            Me.Settings("BaseRomProject") = "BaseRom"
            Me.Settings("ModPackProject") = "ModPack"
            CreateProject("", "BaseRom", GetType(BaseRomProject), CurrentPluginManager)
            CreateProject("", "ModPack", GetType(DSModPackProject), CurrentPluginManager)
            Settings("IsInitialLoadComplete") = True
        End If
    End Function

    Public Overrides Async Function Build() As Task
        Dim info As ModpackInfo = Me.Settings("ModpackInfo")
        If info Is Nothing Then
            info = New ModpackInfo
            Me.Settings("ModpackInfo") = info
        End If
        Dim baseRomProject As BaseRomProject = GetProjectsByName(Me.Settings("BaseRomProject")).FirstOrDefault
        If baseRomProject IsNot Nothing Then
            info.System = baseRomProject.RomSystem
            info.GameCode = baseRomProject.GameCode
            Me.Settings("System") = info.System
            Me.Settings("GameCode") = info.GameCode
        End If
        Await MyBase.Build()
        'Dim modPacks As New List(Of DSModPackProject)
        'Dim allProjects As New List(Of Project)(Me.GetAllProjects)
        'Dim built As Integer = 0
        'For Each item In allProjects
        '    PluginHelper.SetLoadingStatus(PluginHelper.GetLanguageItem("Building projects..."), built / allProjects.Count)
        '    If TypeOf item Is DSModPackProject Then
        '        modPacks.Add(item)
        '    Else
        '        Await item.Build(Me)
        '        built += 1
        '    End If
        'Next
        'For Each item In modPacks
        '    PluginHelper.SetLoadingStatus(PluginHelper.GetLanguageItem("Building projects..."), built / allProjects.Count)
        '    Await item.Build(Me)
        '    built += 1
        'Next
        'PluginHelper.SetLoadingStatusFinished()
    End Function
End Class
