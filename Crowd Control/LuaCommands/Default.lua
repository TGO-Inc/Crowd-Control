---@class World
---@type Server
---@param sm.player.getAllPlayers()[1] player 
function Default.Aggro( self, params )
    local units = sm.unit.getAllUnits()
    for _, unit in ipairs( units ) do
        sm.event.sendToUnit( unit, "sv_e_receiveTarget", { targetCharacter = params.player.character } )
    end
end

---@class Game
---@type Client
function Default.Give( self, param )
    self:cl_onChatCommand( { "/" .. param } )
end

---@class Player
---@type Server
function Default.Heal( self, params )
    self.sv.saved.stats.hp = 100
    self.storage:save( self.sv.saved )
    self.network:setClientData( self.sv.saved )
end

---@class Player
---@type Server
function Default.Kill( self, params )
    self.sv.respawnInteractionAttempted = false
    self.sv.saved.isConscious = false
    local character = self.player:getCharacter()
    character:setTumbling( true )
    character:setDowned( true )
    self.storage:save( self.sv.saved )
    self.network:setClientData( self.sv.saved )
end

---@class World
---@type Server
---@alias Ambush
function Default.Raid( self, params )
    --sm.event.sendToWorld( self.sv.saved.overworld, "sv_ambush", {wave = true, magnitude = 10} )
    params = {wave = true, magnitude = 2}
    print( "Ambush - magnitude:", params.magnitude, "wave:", params.wave )
	local players = sm.player.getAllPlayers()

	for _, player in pairs( players ) do
		if player.character and player.character:getWorld() == self.world then
			local incomingUnits = {}

			local playerPosition = player.character.worldPosition
			local playerDensity = g_unitManager:sv_getPlayerDensity( playerPosition )

			if params.wave then
                for i = 0, params.magnitude do
				    incomingUnits[#incomingUnits + 1] = unit_haybot
                    incomingUnits[#incomingUnits + 1] = unit_haybot
                    incomingUnits[#incomingUnits + 1] = unit_totebot_green
                    incomingUnits[#incomingUnits + 1] = unit_totebot_green
                end
			end

			local minDistance = 20
			local maxDistance = 32 -- 128 is maximum guaranteed terrain distance
			local validNodes = sm.pathfinder.getSortedNodes( playerPosition, minDistance, maxDistance )
			local validNodesCount = table.maxn( validNodes )

			--local incomingUnits = g_unitManager:sv_getRandomUnits( unitCount, playerPosition )

			if validNodesCount > 0 then
				print( #incomingUnits .. " enemies are approaching!" )
				for i = 1, #incomingUnits do
					local selectedNode = math.random( validNodesCount )
					local unitPos = validNodes[selectedNode]:getPosition()

					local playerDirection = playerPosition - unitPos
					local yaw = math.atan2( playerDirection.y, playerDirection.x ) - math.pi/2

					sm.unit.createUnit( incomingUnits[i], unitPos + sm.vec3.new( 0, 0.1, 0), yaw, { temporary = true, roaming = true, ambush = true, tetherPoint = playerPosition } )
				end
			else
				local maxSpawnAttempts = 32
				for i = 1, #incomingUnits do
					local spawnAttempts = 0
					while spawnAttempts < maxSpawnAttempts do
						spawnAttempts = spawnAttempts + 1
						local distanceFromCenter = math.random( minDistance, maxDistance )
						local spawnDirection = sm.vec3.new( 0, 1, 0 )
						spawnDirection = spawnDirection:rotateZ( math.rad( math.random( 359 ) ) )
						local spawnPosition = playerPosition + spawnDirection * distanceFromCenter

						local success, result = sm.physics.raycast( spawnPosition + sm.vec3.new( 0, 0, 128 ), spawnPosition + sm.vec3.new( 0, 0, -128 ), nil , -1 )
						if success and ( result.type == "limiter" or result.type == "terrainSurface" ) then
							local directionToPlayer = playerPosition - spawnPosition
							local yaw = math.atan2( directionToPlayer.y, directionToPlayer.x ) - math.pi / 2
							spawnPosition = result.pointWorld
							sm.unit.createUnit( incomingUnits[i], spawnPosition, yaw, { temporary = true, roaming = true, ambush = true, tetherPoint = playerPosition } )
							break
						end
					end
				end
			end
		end
	end
end

---@class Game
---@type Server
function Default.Spawn( self, param )
    local spawnParams = {
        uuid = Default.SearchUnitID(param),
        world = self.sv.saved.overworld,
        position = sm.player.getAllPlayers()[1]:getCharacter():getWorldPosition() + sm.vec3.new(sm.noise.randomRange(-20,20),sm.noise.randomRange(-20,20),4),
        yaw = 0.0,
        amount = 1 --TODO: Give Members chance to spawn up to 10?
    }
    self:sv_spawnUnit( spawnParams )
end

---@field Global
function Default.SearchUnitID( unit )
    local uuid = nil
    print(unit)
    if unit == "woc" then
        uuid = unit_woc
    elseif unit == "tapebot"  then
        uuid = unit_tapebot
    elseif unit == "redtapebot" then
        uuid = unit_tapebot_red
    elseif unit == "totebot" then
        uuid = unit_totebot_green
    elseif unit == "haybot" then
        uuid = unit_haybot
    elseif unit == "worm" then
        uuid = unit_worm
    elseif unit == "farmbot"  then
        uuid = unit_farmbot
    end
    return uuid
end

---@class Game
---@type Client
function Default.Import( self, params )
    objName = params
    playerDir = ( sm.vec3.new( 1, 1, 0 ) * sm.camera.getDirection() ) + sm.vec3.new( 0, 0, 2.5 )
    direction = playerDir * 5

    if(type(params)==type({})) then
        objName = params[1]
        random_n = 0
        if params[2] ~= nil then
            if params[2] == "random" then random_n = math.random(1,6) end
            -- front is default and already set
            if params[2] == "above" or params[2] == "over" or random_n == 1 then
                direction = sm.vec3.new( 0, 0, 6 )
            elseif params[2] == "right" or params[2] == "east" or random_n == 2 then
                direction = ( sm.camera.getRight() * 5 ) + sm.vec3.new( 0, 0, 2.5 )
            elseif params[2] == "left" or params[2] == "west" or random_n == 3 then
                direction = ( sm.camera.getRight() * -5 ) + sm.vec3.new( 0, 0, 2.5 )
            elseif params[2] == "behind" or params[2] == "south" or params[2] == "backward" or params[2] == "back" or random_n == 4 then
                direction = ( playerDir * -5 ) + sm.vec3.new( 0, 0, 2.5 )
            elseif params[2] == "on" or params[2] == "up" or random_n == 5 then
                direction = sm.vec3.new( 0, 0, -1 )
            elseif params[2] == "down" or params[2] == "under" or random_n == 6 then
                direction = sm.vec3.new( 0, 0, -6 )
            end
        end
    end

    local pos = sm.localPlayer.getPlayer().character:getWorldPosition() + direction
    local lparams = {
        world = sm.localPlayer.getPlayer().character:getWorld(),
        name = params.obj_name,
        position = pos
    }

    -- TODO add pcall for importing modded creations (it will fail to import)
    --[[
    local modPartsLoaded, err = pcall(sm.item.getShapeSize, sm.uuid.new('cf73bdd4-caab-440d-b631-2cac12c17904'))
    if not modPartsLoaded then
        error('sm.interop is not enabled for this world')
    end
    ]]
    -- sm.creation.importFromFile( lparams.world, download_folder.."/blueprint.json", lparams.position, nil, true )
    self.network:sendToServer("importFromFile", lparams)
end