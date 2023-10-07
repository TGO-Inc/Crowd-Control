---@class Game
---@type Server
---@param sm.localPlayer player 
function Default.Aggro( self, params )
    sm.game.setEnableAggro( true )
    local units = sm.unit.getAllUnits()
    for _, unit in ipairs( units ) do
        sm.event.sendToUnit( unit, "sv_e_receiveTarget", { targetCharacter = params.player.character } )
    end
end

---@class World
---@type Server
---@param sm.localPlayer.worldPosition location 
function Default.Blast( self, params )
    local units = sm.unit.getAllUnits()
    for i, unit in ipairs( units ) do
        if InSameWorld( self.world, unit ) then
            if unit ~= nil then
                local distance = (  unit:getCharacter().worldPosition - params.location ):length()
                if distance < 500 then
                    sm.physics.explode( unit:getCharacter().worldPosition + sm.vec3.new(0,0,0.05) , 10, 5, 15, 25, "RedTapeBot - ExplosivesHit" )
                end
            end
        end
    end
end

---@class Game
---@type Server
---@param sm.localPlayer player
---@field Generic
function Default.Give( self, params )
    sm.container.beginTransaction()
    sm.container.collect( params.player:getInventory(), params.item, params.quantity, false )
    sm.container.endTransaction()
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

---@class Game
---@type Server
---@alias Ambush
function Default.Raid( self, params )
    sm.event.sendToWorld( self.sv.saved.overworld, "sv_ambush", {wave = true, magnitude = 10} )
end

---@class Game
---@type Server
function Default.Spawn( self, params )
    local spawnParams = {
        uuid = sm.uuid.new( "00000000-0000-0000-0000-000000000000" ),
        world =self.world,
        position = self.playerLocation + sm.vec3.new(sm.noise.randomRange(-25,25),sm.noise.randomRange(-25,25),4),
        yaw = 0.0,
        amount = 1 --TODO: Give Members chance to spawn up to 10?
    }
    spawnParams.uuid = Default.SearchUnitID(params[2]) 
    self.network:sendToServer( "sv_spawnUnit", spawnParams )
end

---@field Global
function Default.SearchUnitID(unit)
    local uuid = nil
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
---@type Server
function Default.Import( self, params )
    objName = params
    playerDir = ( sm.vec3.new( 1, 1, 0 ) * sm.camera.getDirection() ) + sm.vec3.new( 0, 0, 2.5 )
    direction = playerDir * 5

    if(type(params)=="table") then
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
    sm.creation.importFromFile( lparams.world, download_folder.."/blueprint.json", lparams.position, nil, true )
end