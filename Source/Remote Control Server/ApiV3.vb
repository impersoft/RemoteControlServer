﻿Module ApiV3

    'API v3 uses commands similar to API v2 but supports communication from the server to an app

    Public Const COMMAND_IDENTIFIER As Byte = 127

    'first index
    Public Const cmd_connection As Byte = 10
    Public Const cmd_disconnect As Byte = 11
    Public Const cmd_pause As Byte = 12
    Public Const cmd_resume As Byte = 13
    Public Const cmd_control As Byte = 14
    Public Const cmd_pin As Byte = 15
    Public Const cmd_broadcast As Byte = 16
    Public Const cmd_set As Byte = 17
    Public Const cmd_get As Byte = 19


    'second index
    'connect
    Public Const cmd_connection_reachable As Byte = 0
    Public Const cmd_connection_protected As Byte = 1
    Public Const cmd_connection_connect As Byte = 2
    Public Const cmd_connection_disconnect As Byte = 3

    'setter
    Public Const cmd_set_pin As Byte = 0
    Public Const cmd_set_app_version As Byte = 1
    Public Const cmd_set_app_name As Byte = 2
    Public Const cmd_set_os_version As Byte = 3
    Public Const cmd_set_os_name As Byte = 4

    Public Const cmd_get_server_version As Byte = 1
    Public Const cmd_get_server_name As Byte = 2
    Public Const cmd_get_os_name As Byte = 4
    Public Const cmd_get_screenshot As Byte = 5
    Public Const cmd_get_api_version As Byte = 6

    Public Const cmd_mouse As Byte = 20
    Public Const cmd_mouse_pointers As Byte = 1
    Public Const cmd_mouse_pad_action As Byte = 2
    Public Const cmd_mouse_left_action As Byte = 3
    Public Const cmd_mouse_right_action As Byte = 4
    Public Const cmd_mouse_action_up As Byte = 0
    Public Const cmd_mouse_action_down As Byte = 1
    Public Const cmd_mouse_action_click As Byte = 2


    Public Function isBroadcast(ByVal command As Command) As Boolean
        If command.data.Equals(New Byte() {COMMAND_IDENTIFIER, cmd_broadcast}) Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub requestPin(ByRef app As App)
        'Send pin request
        Dim command As New Command
        command.source = Network.getServerIp()
        command.destination = app.ip
        command.priority = RemoteControlServer.Command.PRIORITY_HIGH

        Dim serverName As Byte() = New Byte() {COMMAND_IDENTIFIER, cmd_connection, cmd_connection_protected}
        command.data = buildCommandData(serverName, Converter.stringToByte(Server.gui.userName))

        command.send()
    End Sub

    Public Sub answerBroadCast(ByRef app As App)
        'Send server name back
        Dim command As New Command
        command.source = Network.getServerIp()
        command.destination = app.ip
        command.priority = RemoteControlServer.Command.PRIORITY_HIGH

        Dim serverName As Byte() = New Byte() {COMMAND_IDENTIFIER, cmd_get, cmd_get_server_name}
        command.data = buildCommandData(serverName, Converter.stringToByte(Server.gui.userName))

        command.send()
    End Sub

    Public Sub refuseBroadCast(ByRef app As App)

    End Sub

    Public Sub parseCommand(ByRef command As Command)
        Select Case command.data(1)
            Case cmd_connection
                parseConnectCommand(command)
            Case cmd_get
                answerGetRequest(command)
            Case cmd_mouse
                parseMouseCommand(command)
            Case Else
                Logger.add("Unknown command")
        End Select
    End Sub

#Region "Connecting"

    Public Sub parseConnectCommand(ByRef command As Command)
        Dim app As App = Server.getApp(command.source)

        Select Case command.data(2)
            Case cmd_connection_connect
                'App wants to connect with the server       
                app.onConnect()
            Case cmd_connection_reachable
                'App checks if server is reachable
                'Reply with the server name

                If command.data.Length > 2 Then
                    'Command data contains source IP
                    app.ip = Converter.byteToString(command.data, 3)
                End If

                answerBroadCast(app)
                Logger.add(app.ip & " checked reachability")
            Case Else
                Logger.add("Unknown connection command")
        End Select
    End Sub

#End Region

#Region "Get requests"

    Public Sub answerGetRequest(ByRef requestCommand As Command)
        Dim app As App = Server.getApp(requestCommand.source)

        Dim responseCommand As New Command
        responseCommand.source = Network.getServerIp()
        responseCommand.destination = app.ip
        responseCommand.priority = RemoteControlServer.Command.PRIORITY_HIGH

        Dim commandIdentifier As Byte() = New Byte() {COMMAND_IDENTIFIER, cmd_get, requestCommand.data(2)}

        Select Case requestCommand.data(2)
            Case cmd_get_server_version
                responseCommand.data = buildCommandData(commandIdentifier, Converter.stringToByte(Server.getServerVersionName))
            Case cmd_get_server_name
                responseCommand.data = buildCommandData(commandIdentifier, Converter.stringToByte(Server.gui.userName))
            Case cmd_get_os_name
                responseCommand.data = buildCommandData(commandIdentifier, Converter.stringToByte(Server.getServerOs))
            Case cmd_get_api_version
                responseCommand.data = buildCommandData(commandIdentifier, New Byte() {3})
            Case cmd_get_screenshot
                Dim width As Integer = 9999
                Dim quality As Integer = Settings.screenQuality

                If requestCommand.data.Length >= 3 Then
                    width = requestCommand.data(3) * 10

                    If requestCommand.data.Length >= 4 Then
                        width = requestCommand.data(4)
                    End If
                End If

                Dim screenshotData As Byte() = Converter.bitmapToByte(Screenshot.getResizedScreenshot(width), quality)
                responseCommand.data = buildCommandData(commandIdentifier, screenshotData)
            Case Else
                Logger.add("Unknown get command")
        End Select

        If Not responseCommand.data Is Nothing Then
            responseCommand.send()
        End If
    End Sub

#End Region

#Region "Mouse"

    Public Sub parseMouseCommand(ByRef command As Command)
        Dim app As App = Server.getApp(command.source)

        Select Case command.data(2)
            Case cmd_mouse_pointers
                MouseV3.parsePointerData(command.data)
            Case cmd_mouse_pad_action
                Select Case command.data(3)
                    Case cmd_mouse_action_down
                        Logger.add("Mouse pad down")
                    Case Else
                        Logger.add("Unknown mouse pad command")
                End Select
            Case Else
                Logger.add("Unknown mouse command")
        End Select
    End Sub

#End Region

End Module
