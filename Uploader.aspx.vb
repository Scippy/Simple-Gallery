'
' Simple Gallery for DotNetNuke -  http://www.dotnetnuke.com
' Copyright (c) 2002-2007
' by Ventrian ( sales@ventrian.com ) ( http://www.ventrian.com )
'

Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO

Imports DotNetNuke.Common
Imports DotNetNuke.Common.Utilities
Imports DotNetNuke.Entities.Modules
Imports DotNetNuke.Entities.Portals
Imports DotNetNuke.Entities.Tabs
Imports DotNetNuke.Entities.Users
Imports DotNetNuke.Security
Imports DotNetNuke.Security.Roles
Imports DotNetNuke.Services.FileSystem

Imports Ventrian.SimpleGallery.Common
Imports Ventrian.SimpleGallery.Entities
Imports DotNetNuke.Services.Log.EventLog
Imports DotNetNuke.Security.Permissions

Imports Ventrian.ImageResizer

Namespace Ventrian.SimpleGallery

    Partial Public Class Uploader
        Inherits SimpleGalleryPageBase

#Region " Private Members "

        Private _moduleConfiguration As ModuleInfo
        Private _moduleID As Integer = Null.NullInteger
        Private _tabModuleID As Integer = Null.NullInteger
        Private _tabID As Integer = Null.NullInteger
        Private _portalID As Integer = Null.NullInteger
        Private _ticket As String = Null.NullString
        Private _userID As Integer = Null.NullInteger

        Private _settings As Hashtable
        Private _portal As PortalInfo
        Private _tab As TabInfo

#End Region

#Region " Private Properties "

        Private ReadOnly Property Tab() As TabInfo
            Get
                If (_tab Is Nothing) Then
                    Dim objTabController As New TabController
                    _tab = objTabController.GetTab(_tabID, _portalID, False)
                End If
                Return _tab
            End Get
        End Property

        Private ReadOnly Property Portal() As PortalInfo

            Get

                If (_portal Is Nothing) Then
                    Dim objPortalController As New PortalController
                    _portal = objPortalController.GetPortal(_portalID)
                End If
                Return _portal

            End Get

        End Property

        Private ReadOnly Property UploadSettings() As GallerySettings

            Get

                Return Me.GallerySettings(Settings)

            End Get

        End Property

        Private ReadOnly Property ModuleConfiguration() As ModuleInfo

            Get

                If _moduleConfiguration Is Nothing Then

                    Dim objModule As New ModuleController
                    _moduleConfiguration = objModule.GetModule(_moduleID, _tabID, False)

                End If

                Return _moduleConfiguration
            End Get

        End Property

        Private ReadOnly Property Settings() As Hashtable

            Get

                If _settings Is Nothing Then

                    Dim objModuleController As New ModuleController

                    _settings = ModuleController.Instance.GetModule(_moduleID, -1, False).ModuleSettings 'objModuleController.GetModuleSettings(_moduleID)
                    _settings = GetTabModuleSettings(_tabModuleID, _settings)

                End If

                Return _settings
            End Get

        End Property

        Private Function GetTabModuleSettings(ByVal TabModuleId As Integer, ByVal settings As Hashtable) As Hashtable

            Dim dr As IDataReader = DotNetNuke.Data.DataProvider.Instance().GetTabModuleSettings(TabModuleId)

            While dr.Read()

                If Not dr.IsDBNull(1) Then
                    settings(dr.GetString(0)) = dr.GetString(1)
                Else
                    settings(dr.GetString(0)) = ""
                End If

            End While

            dr.Close()

            Return settings

        End Function

#End Region

        Public Class MemoryFile
            Inherits HttpPostedFileBase

            Private _stream As Stream
            Private _contentType As String
            Private _fileName As String

            Public Sub New(ByVal stream As Stream, ByVal contentType As String, ByVal fileName As String)
                Me._stream = stream
                Me._contentType = contentType
                Me._fileName = fileName
            End Sub

            Public Sub SetStream(ByVal stream As Stream)
                Me._stream = stream
            End Sub

            Public Overrides ReadOnly Property ContentLength As Integer
                Get
                    Return CInt(_stream.Length)
                End Get
            End Property

            Public Overrides ReadOnly Property ContentType As String
                Get
                    Return _contentType
                End Get
            End Property

            Public Overrides ReadOnly Property FileName As String
                Get
                    Return _fileName
                End Get
            End Property

            Public Overrides ReadOnly Property InputStream As Stream
                Get
                    Return _stream
                End Get
            End Property

            Public Overrides Sub SaveAs(ByVal filename As String)
                Dim fs As New FileStream(filename, FileMode.Create)
                _stream.CopyTo(fs)
            End Sub
        End Class
#Region " Private Methods "

        Private Sub AuthenticateUserFromTicket()

            If (_ticket <> "") Then

                Dim ticket As FormsAuthenticationTicket = FormsAuthentication.Decrypt(_ticket)
                Dim fi As FormsIdentity = New FormsIdentity(ticket)

                Dim roles As String() = Nothing
                HttpContext.Current.User = New System.Security.Principal.GenericPrincipal(fi, roles)

                Dim objUser As UserInfo = UserController.GetUserByName(_portalID, HttpContext.Current.User.Identity.Name)

                If Not (objUser Is Nothing) Then
                    _userID = objUser.UserID
                    HttpContext.Current.Items("UserInfo") = objUser



                    Dim objRoleController As New RoleController
                    Dim lRoles As IList(Of DotNetNuke.Entities.Users.UserRoleInfo) = objRoleController.GetUserRoles(objUser, True)
                    Dim _myroles As New ArrayList
                    For Each role As DotNetNuke.Entities.Users.UserRoleInfo In lRoles
                        _myroles.Add(role.RoleName)
                    Next
                    'roles = objRoleController.GetRolesByUser(_userID, _portalID)
                    roles = _myroles.ToArray()

                    Dim strPortalRoles As String = Join(roles, New Char() {";"c})
                    Context.Items.Add("UserRoles", ";" + strPortalRoles + ";")
                End If

            End If

        End Sub

        Private Function ExtractFileName(ByVal path As String) As String

            Dim extractPos As Integer = path.LastIndexOf("\") + 1
            Return path.Substring(extractPos, path.Length - extractPos)

        End Function

        Private Function ExtractFileExtension(ByVal fileName As String) As String

            Dim extension As String = ""

            If (fileName.Length > 0) Then
                If (fileName.IndexOf("."c) <> -1) Then
                    If (fileName.LastIndexOf("."c) < fileName.Length) Then
                        extension = fileName.Substring(fileName.LastIndexOf("."c) + 1, fileName.Length - (fileName.LastIndexOf("."c) + 1))
                    End If
                End If
            End If

            Return extension

        End Function

        Public Function GetApproverDistributionList() As Hashtable

            Dim userList As Hashtable = New Hashtable

            If (Me.Settings.Contains(Constants.SETTING_APPROVE_ROLES)) Then

                Dim roles As String = Settings(Constants.SETTING_APPROVE_ROLES).ToString()
                Dim rolesArray() As String = roles.Split(Convert.ToChar(";"))

                For Each role As String In rolesArray
                    If (role.Length > 0) Then
                        Dim objRoleController As RoleController = New RoleController
                        Dim objRole As RoleInfo = objRoleController.GetRoleByName(_portalID, role)

                        If Not (objRole Is Nothing) Then
                            Dim objUsers As IList(Of DotNetNuke.Entities.Users.UserInfo) = objRoleController.GetUsersByRole(_portalID, role)
                            For Each objUser As UserInfo In objUsers
                                If (userList.Contains(objUser.UserID) = False) Then
                                    Dim objUserController As UserController = New UserController
                                    Dim objSelectedUser As UserInfo = objUserController.GetUser(_portalID, objUser.UserID)
                                    If Not (objSelectedUser Is Nothing) Then
                                        If (objSelectedUser.Email.Length > 0) Then
                                            userList.Add(objUser.UserID, objSelectedUser.Email)
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next

            End If

            Return userList

        End Function

        Private Function GetFilePath(ByVal albumID As Integer) As String

            Dim filePath As String = ""

            Dim objAlbumController As New AlbumController
            Dim objAlbum As AlbumInfo = objAlbumController.Get(albumID)

            If Not (objAlbum Is Nothing) Then
                filePath = Portal.HomeDirectoryMapPath & objAlbum.HomeDirectory & "\"
            End If

            If Not (Directory.Exists(filePath)) Then
                Directory.CreateDirectory(filePath)
            End If

            Return filePath

        End Function

        Private Function GetPhotoHeight(ByVal dataItem As Object) As String

            Dim objPhoto As PhotoInfo = CType(dataItem, PhotoInfo)

            If Not (objPhoto Is Nothing) Then
                Dim width As Integer
                If (objPhoto.Width > UploadSettings.ThumbnailWidth) Then
                    width = UploadSettings.ThumbnailWidth
                Else
                    width = objPhoto.Width
                End If

                Dim height As Integer = Convert.ToInt32(objPhoto.Height / (objPhoto.Width / width))
                If (height > UploadSettings.ThumbnailHeight) Then
                    height = UploadSettings.ThumbnailHeight
                    width = Convert.ToInt32(objPhoto.Width / (objPhoto.Height / height))
                End If

                Return height.ToString()
            Else
                Return UploadSettings.ThumbnailWidth.ToString()
            End If

        End Function

        Private Function GetPhotoWidth(ByVal dataItem As Object) As String

            Dim objPhoto As PhotoInfo = CType(dataItem, PhotoInfo)

            If Not (objPhoto Is Nothing) Then
                Dim width As Integer
                If (objPhoto.Width > UploadSettings.ThumbnailWidth) Then
                    width = UploadSettings.ThumbnailWidth
                Else
                    width = objPhoto.Width
                End If

                Dim height As Integer = Convert.ToInt32(objPhoto.Height / (objPhoto.Width / width))
                If (height > UploadSettings.ThumbnailHeight) Then
                    height = UploadSettings.ThumbnailHeight
                    width = Convert.ToInt32(objPhoto.Width / (objPhoto.Height / height))
                End If

                Return width.ToString()
            Else
                Return UploadSettings.ThumbnailWidth.ToString()
            End If

        End Function

        Private Function HasApproval() As Boolean

            If (Me.HasEditPermissions Or Me.HasEditPhotoPermissions Or Me.HasApprovePhotoPermissions) Then
                Return True
            End If

            'If (IsInRoles(ModuleConfiguration.AuthorizedEditRoles) Or IsInRoles(Tab.AdministratorRoles) Or IsInRoles(Portal.AdministratorRoleName)) Then
            If HasEditPermissions() Then
                Return True
            End If

            If (Settings.Contains(Constants.SETTING_EDIT_ROLES)) Then
                If (IsInRoles(Settings(Constants.SETTING_EDIT_ROLES).ToString())) Then
                    Return True
                End If
            End If

            If (Settings.Contains(Constants.SETTING_APPROVE_ROLES)) Then
                If (IsInRoles(Settings(Constants.SETTING_APPROVE_ROLES).ToString())) Then
                    Return True
                End If
            End If

        End Function

        Public Function HasApprovePhotoPermissions() As Boolean

            If (HasEditPermissions()) Then
                Return True
            End If

            If (Settings.Contains(Constants.SETTING_APPROVE_ROLES)) Then
                Return PortalSecurity.IsInRoles(Settings(Constants.SETTING_APPROVE_ROLES).ToString())
            Else
                Return False
            End If

        End Function

        Public Function HasEditPermissions() As Boolean

            Return ModulePermissionController.CanEditModuleContent(ModuleConfiguration)

            'Return _
            '    (PortalSecurity.IsInRoles(ModuleConfiguration.AuthorizedEditRoles) = True) Or
            '    (PortalSecurity.IsInRoles(Tab.AdministratorRoles) = True) Or
            '    (PortalSecurity.IsInRoles(Portal.AdministratorRoleName) = True)

        End Function

        Public Function HasEditPhotoPermissions() As Boolean

            If (HasEditPermissions()) Then
                Return True
            End If

            If (Settings.Contains(Constants.SETTING_EDIT_ROLES)) Then
                Return PortalSecurity.IsInRoles(Settings(Constants.SETTING_EDIT_ROLES).ToString())
            Else
                Return False
            End If

        End Function

        Private Function IsInRole(ByVal role As String) As Boolean

            Dim objUserInfo As UserInfo = UserController.Instance.GetCurrentUserInfo()
            Dim context As HttpContext = HttpContext.Current

            If (role <> "" AndAlso Not role Is Nothing AndAlso ((context.Request.IsAuthenticated = False And role = glbRoleUnauthUserName))) Then
                Return True
            Else
                Dim roles As String = CType(context.Items("UserRoles"), String)
                If Not roles Is Nothing Then
                    Dim rolesArr As String() = roles.Split(";"c)
                    For Each strRole As String In rolesArr
                        If strRole = role Then
                            Return True
                        End If
                    Next
                End If
                Return False
            End If

        End Function

        Private Function IsInRoles(ByVal roles As String) As Boolean

            If Not roles Is Nothing Then
                Dim context As HttpContext = HttpContext.Current
                Dim objUserInfo As UserInfo = UserController.Instance.GetCurrentUserInfo()
                Dim role As String

                For Each role In roles.Split(New Char() {";"c})
                    If objUserInfo.IsSuperUser Or (role <> "" AndAlso Not role Is Nothing AndAlso
                     (role = glbRoleAllUsersName Or
                     IsInRole(role) = True
                     )) Then
                        Return True
                    End If
                Next role

            End If

            Return False

        End Function

        Private Sub ReadQueryString()

            If (Request("ModuleID") <> "") Then
                _moduleID = Convert.ToInt32(Request("ModuleID"))
            End If

            If (Request("PortalID") <> "") Then
                _portalID = Convert.ToInt32(Request("PortalID"))
            End If

            If (Request("TabID") <> "") Then
                _tabID = Convert.ToInt32(Request("TabID"))
            End If

            If (Request("Ticket") <> "") Then
                _ticket = Request("Ticket")
            End If

        End Sub

        Private Function RemoveExtension(ByVal fileName As String) As String

            Dim name As String = ""

            If (fileName.Length > 0) Then
                If (fileName.IndexOf("."c) <> -1) Then
                    name = fileName.Substring(0, fileName.LastIndexOf("."c))
                End If
            End If

            Return name

        End Function

#End Region

#Region " Event Handlers "

        Private Function convertbytetoimage(ByVal BA As Byte())
            Dim ms As MemoryStream = New MemoryStream(BA)
            Dim image = System.Drawing.Image.FromStream(ms)
            Return image
        End Function

        Private Function ToStream(ByVal image As Image, ByVal format As ImageFormat) As Stream
            Dim stream = New System.IO.MemoryStream()
            image.Save(stream, format)
            stream.Position = 0
            Return stream
        End Function

        Private Function FromString(ByVal format As String) As ImageFormat
            Select Case format
                Case "image/jpg", "image/jpeg" : Return ImageFormat.Jpeg
                Case "image/gif" : Return ImageFormat.Gif
                Case "image/png" : Return ImageFormat.Png
                Case Else : Return ImageFormat.Jpeg
            End Select

        End Function

        Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

            Try

                ReadQueryString()
                AuthenticateUserFromTicket()

                If (Request.IsAuthenticated = False) Then
                    Response.Write("-2")
                    Response.End()
                End If

                Dim objUser As UserInfo = UserController.GetUserByName(_portalID, HttpContext.Current.User.Identity.Name)

                If Not (objUser Is Nothing) Then
                    _userID = objUser.UserID
                Else
                    Response.Write("-2")
                    Response.End()
                End If

                Dim Fdata As String = ""
                If (Request("Fdata") <> "") Then
                    Fdata = Request("Fdata")
                End If

                Dim myFileName As String = ""
                If (Request("FileName") <> "") Then
                    myFileName = Request("FileName")
                End If

                Dim myContentType As String = ""
                If (Request("ContentType") <> "") Then
                    myContentType = Request("ContentType")
                End If




                Dim byteresponse As Byte() = Convert.FromBase64String(Fdata)

                Dim img1 As System.Drawing.Image = convertbytetoimage(byteresponse)

                'Dim stream2 As MemoryStream = New MemoryStream(byteresponse)
                Dim stream2 As MemoryStream = ToStream(img1, FromString(myContentType))

                'Dim tream As Stream = Request.InputStream
                'Dim bytes As Byte() = New Byte(tream.Length) {}
                'tream.Position = 0
                'tream.Read(bytes, 0, DirectCast(tream.Length, Long))
                'Dim data As String = Encoding.ASCII.GetString(bytes) ' this is your string

                'Dim js As Script.Serialization.JavaScriptSerializer = New Script.Serialization.JavaScriptSerializer()
                'Dim root As fileInfo = js.Deserialize(Of fileInfo)(fdata1)


                '  Dim fdata As fileInfo = CType(fdata1, fileInfo)
                ' Dim objFile As HttpPostedFile = Request.Files("Filedata")
                'Dim objFile As HttpPostedFile = Request.Files("Filedata")
                Dim objFile As MemoryFile = New MemoryFile(stream2, myContentType, myFileName)

                If Not (objFile Is Nothing) Then

                    Dim objPortalController As New PortalController()
                    If (objPortalController.HasSpaceAvailable(_portalID, objFile.ContentLength) = False) Then
                        Response.Write("-1")
                        Response.End()
                    End If

                    Dim albumID As Integer = Convert.ToInt32(Request("AlbumID"))
                    Dim batchID As String = Request("BatchID")

                    Dim fileName As String = ExtractFileName(objFile.FileName)
                    Dim fileExtension As String = ExtractFileExtension(fileName)
                    Dim fileNameWithoutExtension As String = RemoveExtension(fileName).Replace("/", "_").Replace(".", "_").Replace("%", "_").Replace("+", "")

                    fileName = fileNameWithoutExtension & "." & fileExtension

                    Dim filePath As String = GetFilePath(albumID)

                    If (File.Exists(filePath & fileName)) Then
                        For i As Integer = 1 To 1000
                            If Not (File.Exists(filePath & fileNameWithoutExtension & "_" & i.ToString() & "." & fileExtension)) Then
                                fileName = fileNameWithoutExtension & "_" & i.ToString() & "." & fileExtension
                                fileNameWithoutExtension = fileNameWithoutExtension & "_" & i.ToString()
                                Exit For
                            End If
                        Next
                    End If

                    Dim objQueryString As New NameValueCollection()
                    objQueryString.Add("maxwidth", UploadSettings.ImageWidth.ToString())
                    objQueryString.Add("maxheight", UploadSettings.ImageHeight.ToString())

                    'objFile.SaveAs(filePath & fileName)
                    img1.Save(filePath & fileName, FromString(myContentType))

                    Dim buff As Byte() = System.IO.File.ReadAllBytes(filePath & fileName)
                    Dim ms As System.IO.MemoryStream = New System.IO.MemoryStream(buff)
                    ms.Position = 0
                    objFile.SetStream(ms)



                    Dim resize As Boolean = False
                    Dim photo As Drawing.Image = Drawing.Image.FromStream(objFile.InputStream)

                    If (UploadSettings.ImageWidth < photo.Width Or UploadSettings.ImageHeight < photo.Height) Then
                        resize = True
                    End If

                    Dim objWatermarkSettings As New WatermarkSettings(Request.QueryString)
                    If (UploadSettings.UseWatermark And UploadSettings.WatermarkText <> "") Then
                        objWatermarkSettings.WatermarkText = UploadSettings.WatermarkText
                    End If
                    If (UploadSettings.UseWatermark And UploadSettings.WatermarkImage <> "") Then
                        objWatermarkSettings.WatermarkImagePath = (PortalSettings.HomeDirectoryMapPath & UploadSettings.WatermarkImage)
                        objWatermarkSettings.WatermarkImagePosition = UploadSettings.WatermarkImagePosition
                    End If

                    Dim target As String = filePath & fileName
                    If (resize And UploadSettings.ResizePhoto) Then
                        ImageManager.getBestInstance().BuildImage(filePath & fileName, target, objQueryString, objWatermarkSettings)
                    End If

                    Dim width As Integer = photo.Width
                    Dim height As Integer = photo.Height

                    If (UploadSettings.ResizePhoto) Then

                        If (width > UploadSettings.ImageWidth) Then
                            width = UploadSettings.ImageWidth
                            height = Convert.ToInt32(height / (photo.Width / UploadSettings.ImageWidth))
                        End If

                        If (height > UploadSettings.ImageHeight) Then
                            height = UploadSettings.ImageHeight
                            width = Convert.ToInt32(photo.Width / (photo.Height / UploadSettings.ImageHeight))
                        End If

                    End If

                    photo.Dispose()

                    Dim objPhoto As New PhotoInfo
                    Dim objPhotoController As New PhotoController

                    objPhoto.Name = RemoveExtension(ExtractFileName(objFile.FileName))
                    objPhoto.ModuleID = _moduleID
                    objPhoto.AlbumID = albumID
                    objPhoto.AuthorID = _userID
                    objPhoto.DateCreated = DateTime.Now
                    objPhoto.DateUpdated = objPhoto.DateCreated

                    objPhoto.FileName = fileNameWithoutExtension & "." & fileExtension
                    objPhoto.Width = width
                    objPhoto.Height = height

                    Try
                        If UploadSettings.UseXmpExif Then
                            Dim objXmpReader As New Entities.MetaData.XmpReader()
                            objXmpReader.ApplyAttributes(objPhoto, objFile.InputStream)
                        End If
                    Catch
                        ' Many things can go wrong here, so just ignore if we can't extract XMP data.
                    End Try

                    DataCache.RemoveCache("SG-Album-Zip-" & albumID)

                    ' Clear Zip Cache
                    Dim objAlbumController As New AlbumController()
                    Dim objAlbum As AlbumInfo = objAlbumController.Get(albumID)

                    While (objAlbum IsNot Nothing)
                        DataCache.RemoveCache("SG-Album-Zip-" & objAlbum.AlbumID.ToString())
                        objAlbum = objAlbumController.Get(objAlbum.ParentAlbumID)
                    End While

                    If (UploadSettings.PhotoModeration) Then
                        If (HasApproval()) Then
                            objPhoto.IsApproved = True
                            objPhoto.DateApproved = objPhoto.DateCreated
                            objPhoto.ApproverID = _userID
                        Else
                            objPhoto.IsApproved = False
                            objPhoto.DateApproved = Null.NullDate
                            objPhoto.ApproverID = Null.NullInteger
                        End If
                    Else
                        objPhoto.IsApproved = True
                        objPhoto.DateApproved = objPhoto.DateCreated
                        objPhoto.ApproverID = _userID
                    End If

                    objPhoto.BatchID = batchID
                    objPhoto.PhotoID = objPhotoController.Add(objPhoto)

                    If (objPhoto.Tags <> "") Then
                        Dim tags As String() = objPhoto.Tags.Split(","c)
                        For Each tag As String In tags
                            If (tag <> "") Then
                                Dim objTagController As New TagController
                                Dim objTag As TagInfo = objTagController.Get(_moduleID, tag.ToLower())

                                If (objTag Is Nothing) Then
                                    objTag = New TagInfo
                                    objTag.ModuleID = _moduleID
                                    objTag.Name = tag
                                    objTag.NameLowered = tag.ToLower()
                                    objTag.Usages = 0
                                    objTag.TagID = objTagController.Add(objTag)
                                End If

                                objTagController.Add(objPhoto.PhotoID, objTag.TagID)
                            End If
                        Next
                    End If

                    ' Re-get the photo to get AlbumPath.
                    objPhoto = objPhotoController.Get(objPhoto.PhotoID)

                    If (UploadSettings.Compression = CompressionType.MinSize) Then
                        Response.Write(Me.ResolveUrl("ImageHandler.ashx?width=" & GetPhotoWidth(CType(objPhoto, Object)) & "&height=" & GetPhotoHeight(CType(objPhoto, Object)) & "&HomeDirectory=" & System.Uri.EscapeDataString(DotNetNuke.Common.Globals.ApplicationPath + "/" + Portal.HomeDirectory + "/" & objPhoto.HomeDirectory) & "&fileName=" & System.Uri.EscapeDataString(objPhoto.FileName) & "&portalid=" & _portalID.ToString() & "&i=" & objPhoto.PhotoID))
                    Else
                        Response.Write(Me.ResolveUrl("ImageHandler.ashx?width=" & GetPhotoWidth(CType(objPhoto, Object)) & "&height=" & GetPhotoHeight(CType(objPhoto, Object)) & "&HomeDirectory=" & System.Uri.EscapeDataString(DotNetNuke.Common.Globals.ApplicationPath + "/" + Portal.HomeDirectory + "/" & objPhoto.HomeDirectory) & "&fileName=" & System.Uri.EscapeDataString(objPhoto.FileName) & "&portalid=" & _portalID.ToString() & "&i=" & objPhoto.PhotoID & "&q=1"))
                    End If

                    Try

                        ' Update DNN File Meta Info
                        Dim strFileName As String = Path.GetFileName(filePath & fileName)
                        Dim strFolderpath As String = GetSubFolderPath(filePath & fileName, _portalID)
                        'Dim finfo As New System.IO.FileInfo(filePath & fileName)

                        'Dim strContentType As String = ""
                        'Dim strExtension As String = Path.GetExtension(fileName).Replace(".", "")

                        'Select Case strExtension
                        '    Case "jpg", "jpeg" : strContentType = "image/jpeg"
                        '    Case "gif" : strContentType = "image/gif"
                        '    Case "png" : strContentType = "image/png"
                        '    Case Else : strContentType = "application/octet-stream"
                        'End Select

                        'Dim folderID As Integer = Null.NullInteger
                        'Dim objFolderController As New FolderController
                        'Dim folder As FolderInfo = objFolderController.GetFolder(_portalID, strFolderpath, False)
                        'If (folder Is Nothing) Then
                        '    folderID = objFolderController.AddFolder(_portalID, strFolderpath)
                        'Else
                        '    folderID = folder.FolderID
                        'End If

                        Dim folderID As Integer = Null.NullInteger
                        'Dim objFolderController As New FolderController
                        Dim folder As FolderInfo = FolderManager.Instance.GetFolder(_portalID, strFolderpath) 'objFolderController.GetFolder(_portalID, strFolderpath, False)
                        If (folder Is Nothing) Then
                            'folderID = objFolderController.AddFolder(_portalID, strFolderpath)
                            folder = FolderManager.Instance.AddFolder(_portalID, strFolderpath)
                        Else
                            folderID = folder.FolderID
                        End If

                        'Dim parentFolderPath As String = strFolderpath.Substring(0, strFolderpath.Substring(0, strFolderpath.Length - 1).LastIndexOf("/") + 1)

                        ''Get Parents permissions
                        'Dim objFolderPermissionController As New FolderPermissionController
                        'Dim objFolderPermissions As FolderPermissionCollection
                        'objFolderPermissions = objFolderPermissionController.GetFolderPermissionsCollectionByFolderPath(_portalID, parentFolderPath)

                        ''Iterate parent permissions to see if permisison has already been added
                        'For Each objPermission As FolderPermissionInfo In objFolderPermissions
                        '    FileSystemUtils.SetFolderPermission(_portalID, folderID, objPermission.PermissionID, objPermission.RoleID, objPermission.UserID, parentFolderPath)
                        'Next

                        'If (strFileName.IndexOf("'") = -1) Then
                        '    Dim objFiles As New FileController
                        '    objFiles.AddFile(_portalID, strFileName, strExtension, finfo.Length, width, height, strContentType, strFolderpath, folderID, True)
                        'End If

                    Catch
                    End Try

                End If

                '  Response.End()


            Catch exc As Exception    'Module failed to load
                Response.Write("-3")
                Dim objEventLog As New EventLogController
                If (exc.InnerException IsNot Nothing) Then
                    objEventLog.AddLog("GalleryUploaderException", exc.InnerException.ToString(), PortalSettings, -1, EventLogController.EventLogType.ADMIN_ALERT)
                End If
                objEventLog.AddLog("GalleryUploaderException", exc.ToString(), PortalSettings, -1, EventLogController.EventLogType.ADMIN_ALERT)
                Response.End()
            End Try

        End Sub

#End Region

    End Class

End Namespace